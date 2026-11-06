using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.DependencyInjection;

using Models;
using Models.DataModels;

using Repository;

using Repository.Tenant.Interfaces;

using Xunit;


namespace UnitTests;


/// <summary>
///     Repository parity evidence for the ported members (tracked insert, range operations, soft versus hard delete,
///     single-property MarkModified) and for tenant scoping that carries no site vocabulary. Every mutation phase ends with an explicit SaveChanges so the evidence does not depend on whether a method saves internally. These tests run against the mapped account model User rather than DemoModel: DemoModel is BaseModel-derived but has no DbSet or migration in this template, so it is not part of the DbContext model, and adding one would invent persistence that neither this template nor novatech's neutral layer has.
/// </summary>
public class RepositoryContractTests : MainWebApplicationFactory<Program>

{


    public RepositoryContractTests() => Setup();


    private sealed class Harness : IDisposable

    {

        public Harness(IServiceScope scope)

        {

            Scope = scope;


            var provider = scope.ServiceProvider;


            Repo = provider.GetRequiredService<IRepository<User>>();

            Context = provider.GetRequiredService<ITenantManager>().GetDbContext();


            Tenants = provider.GetRequiredService<ITenantManager>();

        }


        private IServiceScope Scope { get; }


        public IRepository<User> Repo { get; }

        public ApplicationDbContext Context { get; }


        public ITenantManager Tenants { get; }


        public void Dispose() => Scope.Dispose();

    }


    private Harness OpenHarness() => new Harness(Scope());

    private static User NewUser(string userName) => new User { UserName = userName, Password = "Harness-Password-1" };


    [Fact]
    public async Task InsertAndReturnTrackedEntityReturnsTrackedInstanceWithIdentityAndPersistsWithoutASecondInsert()

    {

        using var harness = OpenHarness();


        var created = await harness.Repo.InsertAndReturnTrackedEntity(NewUser("tracked-insert-placeholder"));


        Assert.True(created.Id > 0);


        created.UserName = "tracked-insert-mutation";


        await harness.Repo.SaveChanges();


        using var verify = OpenHarness();


        Assert.Equal(1, verify.Repo.GetAll().Count(u => u.UserName == "tracked-insert-mutation"));


        Assert.Equal(0, verify.Repo.GetAll().Count(u => u.UserName == "tracked-insert-placeholder"));

    }


    [Fact]
    public async Task SoftDeleteHidesRowsFromQueriesButKeepsThemPhysicalWhileHardDeleteRemovesThemCompletely()

    {

        int softId;

        int hardId;


        using (var seed = OpenHarness())

        {

            var soft = await seed.Repo.InsertAndReturnTrackedEntity(NewUser("soft-deleted-user"));

            var hard = await seed.Repo.InsertAndReturnTrackedEntity(NewUser("hard-deleted-user"));


            await seed.Repo.SaveChanges();


            softId = soft.Id;

            hardId = hard.Id;

        }


        using (var phase = OpenHarness())

        {

            var softTarget = await phase.Repo.Get(softId);

            var hardTarget = await phase.Repo.Get(hardId);


            Assert.NotNull(softTarget);

            Assert.NotNull(hardTarget);


            await phase.Repo.SoftDelete(softTarget);

            await phase.Repo.HardDelete(hardTarget);


            await phase.Repo.SaveChanges();

        }



        using (var hidden = OpenHarness())
        {
            Assert.Null(await hidden.Repo.Get(softId));
            Assert.Null(await hidden.Repo.Get(hardId));



            Assert.DoesNotContain(softId, hidden.Repo.GetAll().Select(u => u.Id));
            Assert.DoesNotContain(hardId, hidden.Repo.GetAll().Select(u => u.Id));
        }



        using var physical = OpenHarness();


        physical.Context.ChangeTracker.Clear();

        var keptRow = physical.Repo.GetAll().IgnoreQueryFilters().SingleOrDefault(u => u.Id == softId);


        Assert.NotNull(keptRow);



        Assert.True(keptRow.IsDeleted);



        Assert.Null(physical.Repo.GetAll().IgnoreQueryFilters().SingleOrDefault(u => u.Id == hardId));

    }


    [Fact]
    public async Task InsertRangeUpdateRangeSoftDeleteRangeAndHardDeleteRangeBehaveOverMultipleEntities()

    {

        using (var seed = OpenHarness())

        {

            await seed.Repo.InsertRange(new List<User>
            {
                NewUser("range-user-one"),
                NewUser("range-user-two"),
                NewUser("range-user-three")
            });


            await seed.Repo.SaveChanges();

        }



        using (var verifyInsert = OpenHarness())
        {
            Assert.Equal(3, verifyInsert.Repo.GetAll().Count(u => u.UserName.StartsWith("range-user")));
        }



        using (var updatePhase = OpenHarness())
        {
            var rows = updatePhase.Repo.GetAll().Where(u => u.UserName.StartsWith("range-user")).ToList();


            foreach (var row in rows) row.UserName = "updated-range-user-" + row.Id;


            await updatePhase.Repo.UpdateRange(rows);


            await updatePhase.Repo.SaveChanges();
        }



        using (var verifyUpdate = OpenHarness())
        {
            Assert.Equal(3, verifyUpdate.Repo.GetAll().Count(u => u.UserName.StartsWith("updated-range-user")));

            Assert.Equal(0, verifyUpdate.Repo.GetAll().Count(u => u.UserName.StartsWith("range-user-")));
        }



        using (var softeningPhase = OpenHarness())
        {
            var victims = softeningPhase.Repo.GetAll()
                .Where(u => u.UserName.StartsWith("updated-range-user"))
                .OrderBy(u => u.Id)
                .Take(2)
                .ToList();


            await softeningPhase.Repo.SoftDeleteRange(victims);


            await softeningPhase.Repo.SaveChanges();
        }



        using (var verifySoftening = OpenHarness())
        {

            Assert.Equal(1, verifySoftening.Repo.GetAll().Count(u => u.UserName.StartsWith("updated-range-user")));



            verifySoftening.Context.ChangeTracker.Clear();

            var physicalRows = verifySoftening.Repo.GetAll()
                .IgnoreQueryFilters()
                .Where(u => u.UserName.StartsWith("updated-range-user"))
                .ToList();


            Assert.Equal(3, physicalRows.Count);

            Assert.Equal(2, physicalRows.Count(u => u.IsDeleted));

        }



        using (var hardeningPhase = OpenHarness())
        {

            var remaining = hardeningPhase.Repo.GetAll()
                .IgnoreQueryFilters()
                .Where(u => u.UserName.StartsWith("updated-range-user"))
                .ToList();


            await hardeningPhase.Repo.HardDeleteRange(remaining);


            await hardeningPhase.Repo.SaveChanges();

        }



        using var verifyHardening = OpenHarness();


        verifyHardening.Context.ChangeTracker.Clear();

        Assert.Empty(verifyHardening.Repo.GetAll().IgnoreQueryFilters().Where(u => u.UserName.StartsWith("updated-range-user")));

    }


    [Fact]
    public async Task MarkModifiedWritesOnlyTheTargetedPropertyOfADetachedInstance()

    {

        int targetId;


        using (var seed = OpenHarness())

        {

            var row = await seed.Repo.InsertAndReturnTrackedEntity(NewUser("mark-modified-original"));


            await seed.Repo.SaveChanges();


            targetId = row.Id;

        }


        using (var patchPhase = OpenHarness())

        {

            patchPhase.Context.ChangeTracker.Clear();

            var carrier = patchPhase.Repo.GetAll().IgnoreQueryFilters().AsNoTracking().Single(u => u.Id == targetId);


            carrier.UserName = "mark-modified-targeted-write";

            carrier.IsDeleted = true;


            patchPhase.Repo.MarkModified(carrier, u => u.UserName);


            await patchPhase.Repo.SaveChanges();

        }


        using var verify = OpenHarness();


        verify.Context.ChangeTracker.Clear();

        var storedRows = verify.Repo.GetAll().IgnoreQueryFilters().Where(u => u.Id == targetId).ToList();


        Assert.Single(storedRows);


        Assert.Equal("mark-modified-targeted-write", storedRows[0].UserName);



        Assert.False(storedRows[0].IsDeleted);

    }


    [Fact]
    public async Task RepositoryQueriesFollowTheCurrentTenantAndCarryNoSiteScopedMembers()

    {

        int scopedId;


        using (var seed = OpenHarness())

        {

            var row = await seed.Repo.InsertAndReturnTrackedEntity(NewUser("tenant-scoped-user"));


            await seed.Repo.SaveChanges();


            scopedId = row.Id;



            Assert.Contains(scopedId, seed.Repo.GetAll().Select(u => u.Id));
        }



        using (var switchedAway = OpenHarness())
        {
            var currentTenant = switchedAway.Tenants.TenantId;
            var otherTenant = currentTenant == 2 ? 3 : 2;



            switchedAway.Tenants.SwitchTenant(otherTenant);



            Assert.DoesNotContain(scopedId, switchedAway.Repo.GetAll().Select(u => u.Id));



            Assert.Null(await switchedAway.Repo.Get(scopedId));
        }



        using (var switchedBack = OpenHarness())
        {
            switchedBack.Tenants.SwitchTenant(1);



            Assert.Contains(scopedId, switchedBack.Repo.GetAll().Select(u => u.Id));
        }
    }
}
