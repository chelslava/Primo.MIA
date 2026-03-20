using FluentAssertions;
using Primo.MIA.Common;
using Xunit;

namespace Primo.MIA.Tests.Database
{
    public class DatabaseTransactionContextTests
    {
        [Fact(DisplayName = "DatabaseTransactionContext: Push/Pop работают как стек")]
        public void DatabaseTransactionContext_PushPop_WorksAsStack()
        {
            while (DatabaseTransactionContext.Current != null)
                DatabaseTransactionContext.Pop();

            DatabaseTransactionContext.Push("tx1");
            DatabaseTransactionContext.Current.Should().Be("tx1");

            DatabaseTransactionContext.Push("tx2");
            DatabaseTransactionContext.Current.Should().Be("tx2");

            DatabaseTransactionContext.Pop();
            DatabaseTransactionContext.Current.Should().Be("tx1");

            DatabaseTransactionContext.Pop();
            DatabaseTransactionContext.Current.Should().BeNull();
        }

        [Fact(DisplayName = "DatabaseTransactionResolver: explicit transactionId имеет приоритет")]
        public void DatabaseTransactionResolver_ExplicitValue_HasPriority()
        {
            while (DatabaseTransactionContext.Current != null)
                DatabaseTransactionContext.Pop();

            DatabaseTransactionContext.Push("ambient");
            DatabaseTransactionResolver.ResolveOptional("explicit").Should().Be("explicit");
            DatabaseTransactionContext.Pop();
        }
    }
}
