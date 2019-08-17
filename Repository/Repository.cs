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
            return entities.Where(e => e.IsDeleted == false).AsQueryable();
        }
        public virtual T Get(int id)
        {
            return entities.Where(e => e.IsDeleted == false).FirstOrDefault(s => s.Id == id);
        }

        public async Task Insert(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "The Inserted Entity is Null");

            entity.AddedDate = DateTime.UtcNow;
            await entities.AddAsync(entity);
            SaveChanges();
        }
        public async Task InsertRange(IEnumerable<T> Entities)
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

        public void Update(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "The Updated entity is null");

            entity.ModifiedDate = DateTime.UtcNow;
            entities.Update(entity);
            SaveChanges();
        }
        public void UpdateRange(IEnumerable<T> Entities)
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

        public void SoftDelete(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "The Deleted Entity Is Null");

            entity.IsDeleted = true;
            entity.DeletedDate = DateTime.UtcNow;
            SaveChanges();
        }
        public void SoftDeleteRange(IEnumerable<T> Entities)
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

        public void HardDelete(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "The Deleted Entity Is Null");
            entity.DeletedDate = DateTime.UtcNow;
            entities.Remove(entity);
            SaveChanges();
        }
        public void HardDeleteRange(IEnumerable<T> Entities)
        {
            if (!Entities.Any())
                throw new ArgumentNullException(nameof(Entities), "The Deleted Entites are Null");

            foreach (var item in Entities)
            {
                item.IsDeleted = true;
                item.DeletedDate = DateTime.UtcNow;
            }
            entities.RemoveRange(Entities);
            SaveChanges();
        }

        public void SaveChanges()
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    context.SaveChanges();
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
