// =============================================================================
// SftpSyncDirectoriesBack.cs — активность «SFTP: Синхронизация каталогов»
// (issue #48).
//
// rsync-паттерн: снимки обоих деревьев → чистый план (размер + mtime, допуск
// 2 с; или только размер) → исполнение. DryRun возвращает план без изменений.
// Опциональное удаление лишнего на приёмнике (DangerCheckBox). После переноса
// mtime выравнивается — повторный прогон ничего не переносит.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;

namespace Primo.MIA
{
    /// <summary>Активность зеркалирования каталога между локальной машиной и SFTP.</summary>
    public class SftpSyncDirectoriesBack : SftpActivityBase<SftpSyncDirectories>
    {
        public override string GroupName { get => ActivityCategories.Sftp; protected set { } }

        // Синхронизация многих файлов — generous таймаут по умолчанию
        protected override int sdkTimeOut { get => 300000; set { } }

        // ── Основные ─────────────────────────────────────────────────────

        private string _propRemoteDir;
        /// <summary>Корень синхронизации на удалённой стороне.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpSyncRemoteDir)]
        public string Prop_RemoteDir
        {
            get => _propRemoteDir;
            set { _propRemoteDir = value; InvokePropertyChanged(this, nameof(Prop_RemoteDir)); }
        }

        private string _propLocalDir;
        /// <summary>Корень синхронизации на локальной машине.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpSyncLocalDir)]
        public string Prop_LocalDir
        {
            get => _propLocalDir;
            set { _propLocalDir = value; InvokePropertyChanged(this, nameof(Prop_LocalDir)); }
        }

        private SftpSyncDirection _propDirection = SftpSyncDirection.Push;
        /// <summary>Направление: Push (локальный → удалённый) или Pull.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpSyncDirection)]
        public SftpSyncDirection Prop_Direction
        {
            get => _propDirection;
            set { _propDirection = value; InvokePropertyChanged(this, nameof(Prop_Direction)); }
        }

        // ── Настройки ────────────────────────────────────────────────────

        private SftpSyncCompareMode _propCompareMode = SftpSyncCompareMode.SizeAndTime;
        /// <summary>Критерий «файл изменился»: размер + время или только размер.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpSyncCompareMode)]
        public SftpSyncCompareMode Prop_CompareMode
        {
            get => _propCompareMode;
            set { _propCompareMode = value; InvokePropertyChanged(this, nameof(Prop_CompareMode)); }
        }

        private bool _propDeleteExtra;
        /// <summary>Удалять на приёмнике то, чего нет в источнике (осторожно!).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpSyncDeleteExtra)]
        public bool Prop_DeleteExtra
        {
            get => _propDeleteExtra;
            set { _propDeleteExtra = value; InvokePropertyChanged(this, nameof(Prop_DeleteExtra)); }
        }

        private bool _propDryRun;
        /// <summary>Сухой прогон: построить план и вернуть сводку, ничего не меняя.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpSyncDryRun)]
        public bool Prop_DryRun
        {
            get => _propDryRun;
            set { _propDryRun = value; InvokePropertyChanged(this, nameof(Prop_DryRun)); }
        }

        // ── Выходные ─────────────────────────────────────────────────────

        private string _propTransferred;
        /// <summary>Число перенесённых файлов.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Sftp_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpSyncTransferred)]
        public string Prop_Transferred
        {
            get => _propTransferred;
            set { _propTransferred = value; InvokePropertyChanged(this, nameof(Prop_Transferred)); }
        }

        private string _propDeleted;
        /// <summary>Число удалённых файлов и каталогов (при DeleteExtra).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Sftp_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpSyncDeleted)]
        public string Prop_Deleted
        {
            get => _propDeleted;
            set { _propDeleted = value; InvokePropertyChanged(this, nameof(Prop_Deleted)); }
        }

        private string _propSkipped;
        /// <summary>Число файлов, совпавших по критерию сравнения.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Sftp_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpSyncSkipped)]
        public string Prop_Skipped
        {
            get => _propSkipped;
            set { _propSkipped = value; InvokePropertyChanged(this, nameof(Prop_Skipped)); }
        }

        private string _propPlanSummary;
        /// <summary>Текстовая сводка плана (при DryRun — план без исполнения).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Sftp_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpSyncPlanSummary)]
        public string Prop_PlanSummary
        {
            get => _propPlanSummary;
            set { _propPlanSummary = value; InvokePropertyChanged(this, nameof(Prop_PlanSummary)); }
        }

        public SftpSyncDirectoriesBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_SftpSyncDirectories;
            sdkComponentHelp = @"Компонент ""SFTP: Синхронизация каталогов""
Зеркалирует каталог между локальной машиной и SFTP: переносит только изменившиеся файлы (rsync-паттерн: сравнение по размеру и времени изменения, повторный прогон ничего не переносит).

Основные:
Удалённый каталог*: [String] Корень синхронизации на SFTP (поддерживается ~).
Локальный каталог*: [String] Корень синхронизации локально.
Направление: [Enum] Push — локальный → удалённый; Pull — удалённый → локальный.

Настройки:
Сравнение: [Enum] Размер + время (по умолчанию) или Только размер.
Удалять лишнее на приёмнике: [Boolean] файлы/каталоги, которых нет в источнике, удаляются (осторожно!).
Сухой прогон: [Boolean] построить план и вернуть сводку, ничего не меняя.

Выходные данные:
Перенесено файлов: [Int32] Число перенесённых файлов (0 при сухом прогоне).
Удалено: [Int32] Число удалённых файлов и каталогов (0 при сухом прогоне).
Пропущено файлов: [Int32] Совпали по критерию — не переносились.
Сводка плана: [String] Человекочитаемая сводка действий.";
            sdkComponentIcon = ActivityIcons.Sftp;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>(ConnectionProperties())
            {
                PropertyBuilder.String("Prop_RemoteDir", "Удалённый каталог (корень синхронизации)"),
                PropertyBuilder.FileSelector("Prop_LocalDir", "Локальный каталог (корень синхронизации)"),
                PropertyBuilder.Enum<SftpSyncDirection>("Prop_Direction", "Направление: Push/Pull"),
                PropertyBuilder.Enum<SftpSyncCompareMode>("Prop_CompareMode", "Критерий сравнения файлов"),
                PropertyBuilder.BooleanObject("Prop_DeleteExtra", "Удалять лишнее на приёмнике"),
                PropertyBuilder.BooleanObject("Prop_DryRun", "Сухой прогон (только план)"),
                PropertyBuilder.Variable<int>("Prop_Transferred", "Перенесено файлов"),
                PropertyBuilder.Variable<int>("Prop_Deleted", "Удалено (файлов и каталогов)"),
                PropertyBuilder.Variable<int>("Prop_Skipped", "Пропущено файлов"),
                PropertyBuilder.Variable<string>("Prop_PlanSummary", "Сводка плана")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var remoteDir = GetPropertyValue<string>(this.Prop_RemoteDir, nameof(Prop_RemoteDir), sd);
                var localDir = GetPropertyValue<string>(this.Prop_LocalDir, nameof(Prop_LocalDir), sd);

                if (string.IsNullOrWhiteSpace(remoteDir))
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = ActivityStrings.Error_SftpSyncRemoteDirRequired };
                if (string.IsNullOrWhiteSpace(localDir))
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = ActivityStrings.Error_SftpSyncLocalDirRequired };

                var logic = new SftpSyncLogic();
                var syncPlan = BuildSyncPlan(logic, sd, remoteDir, localDir);
                if (syncPlan == null)
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = "Не удалось построить план синхронизации" };

                SetVariableValue(this.Prop_Skipped, syncPlan.SkippedCount, sd);
                SetVariableValue(this.Prop_PlanSummary, logic.FormatPlan(syncPlan), sd);

                if (this.Prop_DryRun)
                {
                    // сухой прогон: план без исполнения
                    SetVariableValue(this.Prop_Transferred, 0, sd);
                    SetVariableValue(this.Prop_Deleted, 0, sd);
                    return new ExecutionResult
                    {
                        IsSuccess = true,
                        SuccessMessage = $"[SFTP Sync] план (DryRun): {logic.FormatPlan(syncPlan)}"
                    };
                }

                int transferred = 0;
                int deleted = 0;
                var execution = ExecuteSftp(sd, client =>
                {
                    foreach (var action in syncPlan.Actions)
                    {
                        ExecuteAction(logic, client, action, this.Prop_Direction, localDir, remoteDir);
                        if (action.Kind == SyncActionKind.Transfer)
                            transferred++;
                        else if (action.Kind == SyncActionKind.DeleteFile || action.Kind == SyncActionKind.DeleteDirectory)
                            deleted++;
                    }
                    return new ExecutionResult { IsSuccess = true };
                });

                if (!execution.IsSuccess)
                {
                    execution.ErrorMessage = $"Ошибка синхронизации: {execution.ErrorMessage}";
                    return execution;
                }

                SetVariableValue(this.Prop_Transferred, transferred, sd);
                SetVariableValue(this.Prop_Deleted, deleted, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[SFTP Sync] перенесено: {transferred}, удалено: {deleted}, пропущено: {syncPlan.SkippedCount}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка синхронизации: {ex.Message}" };
            }
        }

        /// <summary>
        /// Строит снимки и план (снимок удалённой стороны требует одной сессии SFTP).
        /// Null — сбой построения (ошибка уже замаплена ExecuteSftp).
        /// </summary>
        private SyncPlan BuildSyncPlan(SftpSyncLogic logic, ScriptingData sd, string remoteDir, string localDir)
        {
            SyncPlan plan = null;

            var result = ExecuteSftp(sd, client =>
            {
                var remote = logic.ListRemoteSnapshot(client, remoteDir);
                var local = logic.ListLocalSnapshot(localDir);

                var source = this.Prop_Direction == SftpSyncDirection.Push ? local : remote;
                var target = this.Prop_Direction == SftpSyncDirection.Push ? remote : local;

                plan = logic.BuildPlan(source, target, this.Prop_DeleteExtra, this.Prop_CompareMode);
                return new ExecutionResult { IsSuccess = true };
            });

            return result.IsSuccess ? plan : null;
        }

        /// <summary>Исполняет одно действие плана против реальных сторон.</summary>
        private void ExecuteAction(
            SftpSyncLogic logic,
            SftpClient client,
            SyncAction action,
            SftpSyncDirection direction,
            string localDir,
            string remoteDir)
        {
            var remotePath = logic.JoinRemotePath(remoteDir, action.RelativePath);
            var localPath = Path.Combine(localDir, action.RelativePath.Replace('/', Path.DirectorySeparatorChar));

            switch (action.Kind)
            {
                case SyncActionKind.CreateDirectory:
                    if (direction == SftpSyncDirection.Push)
                    {
                        if (!client.Exists(remotePath))
                            client.CreateDirectory(remotePath);
                    }
                    else
                    {
                        Directory.CreateDirectory(localPath);
                    }
                    break;

                case SyncActionKind.Transfer:
                    if (direction == SftpSyncDirection.Push)
                    {
                        UploadPreservingMTime(client, localPath, remotePath);
                    }
                    else
                    {
                        var targetDir = Path.GetDirectoryName(localPath);
                        if (!string.IsNullOrEmpty(targetDir))
                            Directory.CreateDirectory(targetDir);
                        using (var stream = File.Create(localPath))
                            client.DownloadFile(remotePath, stream);
                        // Pull: локальное время = удалённому — повторные прогоны ничего не переносят
                        File.SetLastWriteTimeUtc(localPath, client.GetAttributes(remotePath).LastWriteTimeUtc);
                    }
                    break;

                case SyncActionKind.DeleteFile:
                    if (direction == SftpSyncDirection.Push)
                        client.DeleteFile(remotePath);
                    else
                        File.Delete(localPath);
                    break;

                case SyncActionKind.DeleteDirectory:
                    if (direction == SftpSyncDirection.Push)
                        client.DeleteDirectory(remotePath);
                    else
                        Directory.Delete(localPath, false); // план гарантирует пустоту
                    break;
            }
        }

        /// <summary>
        /// Загрузка с сохранением mtime: после переноса время на приёмнике
        /// выставляется равным источнику — сравнение «размер + время» больше
        /// не считает файл изменившимся (идемпотентность). Сохранение mtime —
        /// best effort: критичный перенос выполнен даже при отказе setstat.
        /// </summary>
        private void UploadPreservingMTime(SftpClient client, string localPath, string remotePath)
        {
            var sourceMTimeUtc = File.GetLastWriteTimeUtc(localPath);
            using (var stream = File.OpenRead(localPath))
                client.UploadFile(stream, remotePath);

            try
            {
                var attributes = client.GetAttributes(remotePath);
                attributes.LastWriteTimeUtc = sourceMTimeUtc;
                client.SetAttributes(remotePath, attributes);
            }
            catch (Exception ex)
            {
                // файл перенесён; метка времени не выставилась — при следующем
                // прогоне файл будет перенесён повторно (размер совпадает,
                // решение по времени), это безопасная деградация
                Common.ActivityFileLogger.Warning(
                    sdkComponentName, $"Не удалось сохранить mtime для {remotePath}: {ex.Message}");
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Host, ActivityStrings.Field_SftpHost, ActivityStrings.Error_SftpHostRequired);
            ret.ValidateRequired(this.Prop_RemoteDir, ActivityStrings.Field_SftpSyncRemoteDir, ActivityStrings.Error_SftpSyncRemoteDirRequired);
            ret.ValidateRequired(this.Prop_LocalDir, ActivityStrings.Field_SftpSyncLocalDir, ActivityStrings.Error_SftpSyncLocalDirRequired);
            return ret;
        }
    }
}
