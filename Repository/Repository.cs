using System;

using Microsoft.EntityFrameworkCore;

using Models;

using Models.DataModels;

using OpenTelemetry.Trace;


using Repository.Extensions;

using Repository.Tenant.Interfaces;


using System.Collections.Generic;

using System.Linq;

using System.Linq.Expressions;

using System.Threading;

using System.Threading.Tasks;


namespace Repository;


/// <summary>
///     The single data-access layer of the template. Every mutating operation takes the tenant manager's manipulation queue so concurrent requests cannot interleave SaveChanges on a shared context, and every operation carries a span named Layer&lt;Model&gt;::Method so a trace shows which model each layer touched. The lazy query members deliberately take no span: an IQueryable is only executed by whoever consumes it later, so a span opened here would close before the query ran and report a duration that never happened - the EF Core instrumentation covers those reads with real database spans instead. Nothing here is site scoped: rows are filtered by the tenant the request resolved.  
/// </summary>
public class Repository<T>(ITenantManager tenantManager, Tracer tracer)
    : IRepository<T>
    where T : BaseModel

{

    private ApplicationDbContext Context => tenantManager.GetDbContext();

    private DbSet<T> EntitiesSet => tenantManager.GetDbContext().Set<T>();


    private TelemetrySpan Span(string method)

    {

        var span = tracer.StartActiveSpan(SpanExtensions.SpanName(nameof(Repository), typeof(T), method));

        span.SetAttribute("tenantId", tenantManager.TenantId);

        return span;

    }


    private class ManipulationWait(Semaphore manipulationQueue) : IDisposable

    {

        public static async Task<ManipulationWait> Wait(Semaphore manipulationQueue)

        {

            await Task.Run(manipulationQueue.WaitOne);

            return new ManipulationWait(manipulationQueue);

        }


        public void Dispose() => manipulationQueue.Release();
    }


    public virtual IQueryable<T> GetAll() => EntitiesSet.Where(u => u.TenantId == tenantManager.TenantId);


    public virtual async Task<T> Get(int id)

    {

        using var span = Span(nameof(Get));

        span.SetAttribute("id", id);


        var result = await GetAll().FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantManager.TenantId);


        span.SetAttribute("found", result != null);


        return result;

    }


    public virtual async Task Insert(T entity)

    {

        using var _ = await ManipulationWait.Wait(tenantManager.ManipulationQueue);


        using var span = Span(nameof(Insert));


        await EntitiesSet.AddAsync(entity);

        await SaveChangesHelper();

    }


    public virtual async Task<T> InsertAndReturnTrackedEntity(T entity)

    {

        using var _ = await ManipulationWait.Wait(tenantManager.ManipulationQueue);


        using var span = Span(nameof(InsertAndReturnTrackedEntity));


        await EntitiesSet.AddAsync(entity);

        await SaveChangesHelper();


        // The instance the context is tracking is handed back with its generated id already assigned.  
        span.SetAttribute("id", entity.Id);


        return entity;
    }


    public virtual async Task InsertRange(IEnumerable<T> Entities)

    {

        using var _ = await ManipulationWait.Wait(tenantManager.ManipulationQueue);


        using var span = Span(nameof(InsertRange));


        await EntitiesSet.AddRangeAsync(Entities);
        
        await SaveChangesHelper();
    }


    public virtual async Task Update(T entity)

    {

        using var _ = await ManipulationWait.Wait(tenantManager.ManipulationQueue);


        using var span = Span(nameof(Update));
        
        span.SetAttribute("id", entity.Id);
        
        EntitiesSet.Update(entity);
        
        await SaveChangesHelper();
    }


    public virtual async Task UpdateRange(IEnumerable<T> Entities)

    {

        using var _ = await ManipulationWait.Wait(tenantManager.ManipulationQueue);
        
        
        using var span = Span(nameof(UpdateRange));
        
        
        EntitiesSet.UpdateRange(Entities.ToArray());
        
        
        await SaveChangesHelper();
        
    }


    public virtual async Task SoftDelete(T entity)

    {

        using var _ = await ManipulationWait.Wait(tenantManager.ManipulationQueue);



        using var span = Span(nameof(SoftDelete));



        span.SetAttribute("id", entity.Id);



        entity.IsDeleted = true;



        EntitiesSet.Update(entity);



        await SaveChangesHelper();



    }



    public virtual async Task SoftDeleteRange(IEnumerable<T> Entities)

    {

        using var _ = await ManipulationWait.Wait(tenantManager.ManipulationQueue);



        using var span = Span(nameof(SoftDeleteRange));



        foreach (var item in Entities)

        {

            item.IsDeleted = true;



            EntitiesSet.Update(item);
            
        }



        await SaveChangesHelper();
        
    }


    public virtual async Task HardDelete(T entity)

    {

        using var _ = await ManipulationWait.Wait(tenantManager.ManipulationQueue);


        
 using var span = Span(nameof(HardDelete));


 
   span.SetAttribute("id", entity.Id);


 
   EntitiesSet.Remove(entity);


 
   await SaveChangesHelper();
 
}


    public virtual async Task HardDeleteRange(IEnumerable<T> Entities)

    {

        using var _ = await ManipulationWait.Wait(tenantManager.ManipulationQueue);



 
   using var span = Span(nameof(HardDeleteRange));



 
   EntitiesSet.RemoveRange(Entities);



 
   await SaveChangesHelper();
   
}


    /// <summary>
    ///     Attaches the instance and flags a single property. No queue is taken because nothing is written to the database here; the caller decides when to call SaveChanges. No span either - nothing happens until that SaveChanges, which carries its own.  
    /// </summary>
    public virtual void MarkModified<TP>(T instance, Expression<Func<T, TP>> property) => EntitiesSet.Attach(instance).Property(property).IsModified = true;


    private Task SaveChangesHelper() => Context.SaveChangesAsync();


    public async Task SaveChanges()

    {

        using var _ = await ManipulationWait.Wait(tenantManager.ManipulationQueue);


        
 using var span = Span(nameof(SaveChanges));


        
 await SaveChangesHelper();
    
}

}
