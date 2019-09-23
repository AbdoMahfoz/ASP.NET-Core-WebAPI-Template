using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.DataModels;

namespace Repository
{
    public class Repository<T> : IRepository<T> where T : BaseModel
    {
        protected readonly ApplicationDbContext context;
        protected DbSet<T> entities;

        public Repository(ApplicationDbContext context)
        {
            this.context = context;
            entities = context.Set<T>();
        }

        public virtual IQueryable<T> GetAll()
        {
            return from row in entities
                   where !row.IsDeleted
                   select row;
        }
        public virtual T Get(int id)
        {
            return (from row in GetAll()
                    where row.Id == id
                    select row).SingleOrDefault();
        }

        public virtual async Task Insert(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "The Inserted Entity is Null");

            entity.AddedDate = DateTime.UtcNow;
            await entities.AddAsync(entity);
            SaveChanges();
        }
        public virtual async Task InsertRange(IEnumerable<T> Entities)
        {
            if (!Entities.Any())
                throw new ArgumentNullException(nameof(Entities), "The Inserted Entites are Null");

            foreach (var item in Entities)
            {
                item.AddedDate = DateTime.UtcNow;
            }

            await entities.AddRangeAsync(Entities.ToArray());
            SaveChanges();
        }

        public virtual void Update(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "The Updated entity is null");

            entity.ModifiedDate = DateTime.UtcNow;
            entities.Update(entity);
            SaveChanges();
        }
        public virtual void UpdateRange(IEnumerable<T> Entities)
        {
            if (!Entities.Any())
                throw new ArgumentNullException(nameof(Entities), "Upadted Entites are Null");

            foreach (var item in Entities)
            {
                item.ModifiedDate = DateTime.UtcNow;
            }
            entities.UpdateRange(Entities.ToArray());
            SaveChanges();
        }

        public virtual void SoftDelete(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "The Deleted Entity Is Null");

            entity.IsDeleted = true;
            entity.DeletedDate = DateTime.UtcNow;
            SaveChanges();
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
        }

        public virtual void HardDelete(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "The Deleted Entity Is Null");
            entities.Remove(entity);
            SaveChanges();
        }
        public virtual void HardDeleteRange(IEnumerable<T> Entities)
        {
            if (!Entities.Any())
                throw new ArgumentNullException(nameof(Entities), "The Deleted Entites are Null");
            entities.RemoveRange(Entities);
            SaveChanges();
        }
        public void SaveChanges()
        {
            context.SaveChanges();
            return;
        }
    }
}
