using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Models;
using Models.DataModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repository
{
    public class Repository<T> : IRepository<T> where T : BaseModel
    {
        protected readonly ApplicationDbContext Context;
        protected readonly ILogger Logger;
        protected DbSet<T> Entities;

        public Repository(ApplicationDbContext context, ILogger<Repository<T>> logger)
        {
            Context = context;
            Logger = logger;
            Entities = context.Set<T>();
        }

        public virtual IQueryable<T> GetAll()
        {
            return from row in Entities
                where !row.IsDeleted
                select row;
        }

        public virtual T Get(int id)
        {
            var result = GetAll().Where(e => e.Id == id).FirstOrDefault();
            return result;
        }

        public virtual async Task Insert(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException("Inserted Entity is Null");

            entity.AddedDate = DateTime.UtcNow;
            await Entities.AddAsync(entity);
            SaveChanges();
            Logger.LogInformation($"{entity.GetType()} is added to Database");
        }

        public virtual async Task InsertRange(IEnumerable<T> Entities)
        {
            if (!Entities.Any())
                throw new ArgumentNullException(nameof(Entities), "The Inserted Entites are Null");

            foreach (var item in Entities) item.AddedDate = DateTime.UtcNow;

            await this.Entities.AddRangeAsync(Entities.ToArray());
            SaveChanges();
            Logger.LogInformation($"{Entities.GetType()} are added to Database");
        }

        public virtual void Update(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "The Updated entity is null");

            entity.ModifiedDate = DateTime.UtcNow;
            Entities.Update(entity);
            SaveChanges();
            Logger.LogInformation($"{entity.GetType()} is updated in Database");
        }

        public virtual void UpdateRange(IEnumerable<T> Entities)
        {
            if (!Entities.Any())
                throw new ArgumentNullException(nameof(Entities), "Upadted Entites are Null");

            foreach (var item in Entities) item.ModifiedDate = DateTime.UtcNow;
            this.Entities.UpdateRange(Entities.ToArray());
            SaveChanges();
            Logger.LogInformation($"{Entities.GetType()} are updated in Database");
        }

        public virtual void SoftDelete(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "The Deleted Entity Is Null");

            entity.IsDeleted = true;
            entity.DeletedDate = DateTime.UtcNow;
            SaveChanges();
            Logger.LogInformation($"{entity.GetType()} is softly deleted from Database");
        }

        public virtual void SoftDeleteRange(IEnumerable<T> Entities)
        {
            if (!Entities.Any())
                throw new ArgumentNullException(nameof(Entities), "The Deleted Entites are Null");

            foreach (var item in Entities)
            {
                item.IsDeleted = true;
                item.DeletedDate = DateTime.UtcNow;
            }

            SaveChanges();
            Logger.LogInformation($"{Entities.GetType()} are softly deleted from Database");
        }

        public virtual void HardDelete(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "The Deleted Entity Is Null");
            Entities.Remove(entity);
            SaveChanges();
            Logger.LogInformation($"{entity.GetType()} is removed from Database");
        }

        public virtual void HardDeleteRange(IEnumerable<T> Entities)
        {
            if (!Entities.Any())
                throw new ArgumentNullException(nameof(Entities), "The Deleted Entites are Null");
            this.Entities.RemoveRange(Entities);
            SaveChanges();
            Logger.LogInformation($"{Entities.GetType()} are removed from Database");
        }

        public void SaveChanges()
        {
            Context.SaveChanges();
        }
    }
}