﻿using easyNetAPI.Data.Repository.IRepository;
using easyNetAPI.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using easyNetAPI.Models.UpsertModels;

namespace easyNetAPI.Data.Repository
{
    public class PostRepository : IPostRepository
    {
        private readonly IUserBehaviorRepository _users;
        private readonly IMongoCollection<UserBehavior> _usersCollection;

        public PostRepository(IMongoCollection<UserBehavior> usersCollection, IUserBehaviorRepository users)
        {
            _usersCollection = usersCollection;
            _users = users;
        }

        private async Task<List<Post>> Query()
        {
            var unwindStage = new BsonDocument("$unwind", new BsonDocument { { "path", "$posts" } });
            var replaceRootStage = new BsonDocument("$replaceRoot", new BsonDocument {
 {"newRoot", "$posts"}
 });
            var pipeline = new[] { unwindStage, replaceRootStage };
            var _botsCollection = _usersCollection.Aggregate<BsonDocument>(pipeline).ToList();
            List<Post> posts = new();
            foreach (var bsonDocument in _botsCollection)
            {
                posts.Add(BsonSerializer.Deserialize<Post>(bsonDocument));
            }
            return posts;
        }

        public async Task<List<Post>> GetAllAsync() => await Query();

        public async Task<Post?> GetFirstOrDefault(int postId) => Query().Result.FirstOrDefault(x => x.PostId == postId);

        public async Task AddAsync(Post post, string userId)
        {
            UserBehavior user = _users.GetFirstOrDefault(userId).Result;
            user.Posts.Add(post);
            await _users.UpdateOneAsync(userId, user);
        }

        public async Task RemoveAsync(int postId, string userId)
        {
            Dictionary<string, UserBehavior> dict = new();
            UserBehavior users = _users.GetAllAsync().Result.ToList().Find(x => x.UserId == userId);
            users.Posts.RemoveAll(x => x.PostId == postId);
            dict.Add(userId, users);
            await _users.UpdateManyAsync(dict);
        }

        public async Task<bool> UpdateOneAsync(Post post)
        {
            var userBehavior = await _users.GetFirstOrDefault(post.UserId);
            if (userBehavior is null)
            {
                return false;
            }
            var oldPost = userBehavior.Posts.Where(p => p.PostId == post.PostId).FirstOrDefault();
            if (oldPost is null)
            {
                return false;
            }
            userBehavior.Posts.Remove(oldPost);
            userBehavior.Posts.Add(post);
            await _users.UpdateOneAsync(userBehavior.UserId, userBehavior);
            return true;
        }

        public async Task<string> DeletePost(int postId, string userId)
        {
            //var userBehavior = await _userBehaviorSettings.GetAsync(userId);
            //if (userBehavior is null)
            //{
            //    return "User not found";
            //}
            //var posts = userBehavior.Posts.ToList();
            //if (posts.Count() == 0)
            //{
            //    return "Post not found";
            //}
            //var postFromDb = posts.Where(p => p.PostId == postId).FirstOrDefault();
            //if (postFromDb is null)
            //{
            //    return "Post not found";
            //}
            //posts.Remove(postFromDb);
            //userBehavior.Posts = posts.ToArray();
            //await _userBehaviorSettings.UpdateAsync(userId, userBehavior);
            return "Post deleted successfully";
        }

        public async Task<bool> UpdatePostContentAsync(UpsertPost post, string userId)
        {
            UserBehavior user = _users.GetAllAsync().Result.ToList().Find(x => x.UserId == userId);
            Post postFromDb = user.Posts.Find(x => x.PostId == post.PostId);
            if (postFromDb is not null)
            {
                postFromDb.Content = post.Content;
                await _users.UpdateOneAsync(userId, user);
                return true;
            }
            return false;
        }

        public async Task UpdateManyAsync(Dictionary<int, Post> posts, string userId)
        {
            UserBehavior user = _users.GetFirstOrDefault(userId).Result;
            foreach (var post in posts)
            {
                Post _post = user.Posts.Find(x => x.PostId == post.Key);
                _post = post.Value;
            }
            await _users.UpdateOneAsync(userId, user);
        }
    }
}