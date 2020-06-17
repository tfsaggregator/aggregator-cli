using Xunit;
using XUnitPriorityOrderer;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
// addset the custom test's collection orderer
[assembly: TestCollectionOrderer(CollectionPriorityOrderer.TypeName, CollectionPriorityOrderer.AssembyName)]
