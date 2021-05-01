using MongoDB.Driver;

namespace NdvBot.Database.Mongo
{
    public interface IMongoConnection
    {
        public IMongoDatabase MainDb { get; }
        public IMongoDatabase ServerDb { get; }
        
    }
}