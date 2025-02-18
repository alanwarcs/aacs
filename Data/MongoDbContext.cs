using MongoDB.Driver;
using aacs.Models;
using System;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext()
    {
        var mongoConnectionString = Environment.GetEnvironmentVariable("MONGO_DB_CONNECTION_STRING");
        if (string.IsNullOrEmpty(mongoConnectionString))
        {
            throw new Exception("MongoDB connection string is not set in the environment variables.");
        }

        var client = new MongoClient(mongoConnectionString);
        _database = client.GetDatabase("aacs");
    }

    public IMongoCollection<Admin> Admins => _database.GetCollection<Admin>("Admins");
    public IMongoCollection<Service> Service => _database.GetCollection<Service>("Service");
    public IMongoCollection<Blog> Blog => _database.GetCollection<Blog>("Blog");
    public IMongoCollection<Contact> Contact => _database.GetCollection<Contact>("Contact");
}