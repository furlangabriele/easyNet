﻿using easyNetAPI.Models;
using easyNetAPI.Data.Repository.IRepository;
using Microsoft.Extensions.Hosting;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace easyNetAPI.Data.Repository
{

    public class UnitOfWork : IUnitOfWork
    {
        private readonly IOptions<MongoDbSettings> _settings;
        private readonly IMongoCollection<UserBehavior> _usersCollection;
        public UnitOfWork(IOptions<MongoDbSettings> settings)
        {
            _settings = settings;
            var mongoClient = new MongoClient(
            settings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                settings.Value.DatabaseName);

            _usersCollection = mongoDatabase.GetCollection<UserBehavior>(
                settings.Value.CollectionName);
            UserBehavior = new UserBehaviorRepository(_usersCollection);
            Company = new CompanyRepository(_usersCollection, UserBehavior);
            Bot = new BotRepository(_usersCollection);
            QA = new QARepository(_usersCollection);
            Panel = new PanelRepository(_usersCollection);
            Button = new ButtonRepository(_usersCollection);
            Post = new PostRepository(_usersCollection);
            Comment = new CommentRepository(_usersCollection);
            Reply = new ReplyRepository(_usersCollection);
        }
        public ICompanyRepository Company { get; private set; } = null!;
        public IUserBehaviorRepository UserBehavior { get; private set;}=null!;
        public IBotRepository Bot { get; private set;}=null!;
        public IQARepository QA { get; private set;}=null!;
        public IPanelRepository Panel { get; private set;}=null!;
        public IButtonRepository Button { get; private set;}=null!;
        public IPostRepository Post { get; private set;}=null!;
        public ICommentRepository Comment { get; private set;}=null!;
        public IReplyRepository Reply { get; private set;}=null!;

    }
}