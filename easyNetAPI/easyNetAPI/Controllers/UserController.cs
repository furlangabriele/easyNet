﻿using System;
using System.Data;
using easyNetAPI.Data;
using easyNetAPI.Data.Repository.IRepository;
using easyNetAPI.Models;
using easyNetAPI.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace easyNetAPI.Controllers
{
	[ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly AppDbContext _db;
        private readonly IUnitOfWork _unitOfWork;

        public UserController(ILogger<UserController> logger, AppDbContext db, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _db = db;
            _unitOfWork = unitOfWork;
        }

        [HttpPost("Follow"), Authorize(Roles = SD.ROLE_USER)]
        public async Task<ActionResult<string>> FollowUserAsync(string userName)
        {
            var token = Request.Headers["Authorization"].ToString();
            token = token.Remove(0, 7);
            var principal = await AuthControllerUtility.DecodeJWTToken(token);
            var userId = principal.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" && c.Value.Contains("-")).Value;
            if (userId is null)
            {
                return BadRequest("User not found");
            }

            var managedUser = _db.Users.Where(u => u.Id == userId).FirstOrDefault();
            var userToFollow = _db.Users.Where(u => u.UserName == userName).FirstOrDefault();
            if (userToFollow is null)
            {
                return BadRequest("User not found");
            }

            var managedUserBehavior = await _unitOfWork.UserBehavior.GetFirstOrDefault(managedUser.Id);
            if (managedUserBehavior is null)
            {
                return BadRequest("User not found");
            }

            var followedUserBehavior = await _unitOfWork.UserBehavior.GetFirstOrDefault(userToFollow.Id);
            if (followedUserBehavior is null)
            {
                return BadRequest("User not found");
            }

            managedUserBehavior.FollowedUsers.Add(userToFollow.Id);
            await _unitOfWork.UserBehavior.UpdateOneAsync(managedUserBehavior.UserId, managedUserBehavior);

            followedUserBehavior.FollowersList.Add(managedUser.Id);
            await _unitOfWork.UserBehavior.UpdateOneAsync(followedUserBehavior.UserId, followedUserBehavior);
            return Ok("User followed successfully");    
        }

        [HttpPost("Unfollow"), Authorize(Roles = SD.ROLE_USER)]
        public async Task<ActionResult<string>> UnfollowUserAsync(string userName)
        {
            var token = Request.Headers["Authorization"].ToString();
            token = token.Remove(0, 7);
            var principal = await AuthControllerUtility.DecodeJWTToken(token);
            var userId = principal.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" && c.Value.Contains("-")).Value;
            if (userId is null)
            {
                return BadRequest("User not found");
            }

            var managedUser = _db.Users.Where(u => u.Id == userId).FirstOrDefault();
            var userToFollow = _db.Users.Where(u => u.UserName == userName).FirstOrDefault();
            if (userToFollow is null)
            {
                return BadRequest("User not found");
            }

            var managedUserBehavior = await _unitOfWork.UserBehavior.GetFirstOrDefault(managedUser.Id);
            if (managedUserBehavior is null)
            {
                return BadRequest("User not found");
            }

            var followedUserBehavior = await _unitOfWork.UserBehavior.GetFirstOrDefault(userToFollow.Id);
            if (followedUserBehavior is null)
            {
                return BadRequest("User not found");
            }

            if (managedUserBehavior.FollowedUsers.Count() == 0)
            {
                if (!managedUserBehavior.FollowedUsers.Contains(userToFollow.Id))
                {
                    return BadRequest("Cannot unfollow user");
                }
                return BadRequest("Cannot unfollow user");
            }

            if (followedUserBehavior.FollowersList.Count() == 0)
            {
                if (!followedUserBehavior.FollowersList.Contains(managedUser.Id))
                {
                    return BadRequest("Cannot unfollow user");
                }
                return BadRequest("Cannot unfollow user");
            }

            managedUserBehavior.FollowedUsers.Remove(userToFollow.Id);
            await _unitOfWork.UserBehavior.UpdateOneAsync(managedUserBehavior.UserId, managedUserBehavior);

            followedUserBehavior.FollowersList.Remove(managedUser.Id);
            await _unitOfWork.UserBehavior.UpdateOneAsync(followedUserBehavior.UserId, followedUserBehavior);

            return Ok("User unfollowed successfully");
        }

        //prende i follower di un utente specificato
        [HttpGet("GetUserFollowers"), Authorize(Roles = SD.ROLE_USER)]
        public async Task<List<string>?> GetUserFollowers(string userName)
        {
            var token = Request.Headers["Authorization"].ToString();
            token = token.Remove(0, 7);
            var principal = await AuthControllerUtility.DecodeJWTToken(token);
            var userId = principal.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" && c.Value.Contains("-")).Value;
            if (userId is null)
            {
                return null;
            }

            var userToSearch = _db.Users.Where(u => u.UserName == userName).FirstOrDefault();
            if (userToSearch is null)
            {
                return null;
            }

            var userBehavior = await _unitOfWork.UserBehavior.GetFirstOrDefault(userToSearch.Id);

            var returnList = new List<string>(); 

            if (userBehavior.FollowersList.Count() != 0)
            {
                foreach (var user in userBehavior.FollowersList)
                {
                    returnList.Add(
                        _db.Users.Where(u=> u.Id == user).Select(u => u.UserName).FirstOrDefault()
                        );
                }
            }

            return returnList;
        }

        //prende i follower dell'utente che ha fatto la richiesta
        [HttpGet("GetFollowers"), Authorize(Roles = SD.ROLE_USER)]
        public async Task<List<string>?> GetUserFollowers()
        {
            var token = Request.Headers["Authorization"].ToString();
            token = token.Remove(0, 7);
            var principal = await AuthControllerUtility.DecodeJWTToken(token);
            var userId = principal.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" && c.Value.Contains("-")).Value;
            if (userId is null)
            {
                return null;
            }

            var userBehavior = await _unitOfWork.UserBehavior.GetFirstOrDefault(userId);

            var returnList = new List<string>();

            if (userBehavior.FollowersList.Count() != 0)
            {
                foreach (var user in userBehavior.FollowersList)
                {
                    returnList.Add(
                        _db.Users.Where(u => u.Id == user).Select(u => u.UserName).FirstOrDefault()
                        );
                }
            }

            return returnList;
        }


        //prende i follower di un utente specificato
        [HttpGet("GetUserFollowedList"), Authorize(Roles = SD.ROLE_USER)]
        public async Task<List<string>?> GetUserFollowedList(string userName)
        {
            var token = Request.Headers["Authorization"].ToString();
            token = token.Remove(0, 7);
            var principal = await AuthControllerUtility.DecodeJWTToken(token);
            var userId = principal.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" && c.Value.Contains("-")).Value;
            if (userId is null)
            {
                return null;
            }

            var userToSearch = _db.Users.Where(u => u.UserName == userName).FirstOrDefault();
            if (userToSearch is null)
            {
                return null;
            }

            var userBehavior = await _unitOfWork.UserBehavior.GetFirstOrDefault(userToSearch.Id);

            var returnList = new List<string>();

            if (userBehavior.FollowedUsers.Count() != 0)
            {
                foreach (var user in userBehavior.FollowedUsers)
                {
                    returnList.Add(
                        _db.Users.Where(u => u.Id == user).Select(u => u.UserName).FirstOrDefault()
                        );
                }
            }

            return returnList;
        }

        //prende i followed dell'utente che ha fatto la richiesta
        [HttpGet("GetFollowed"), Authorize(Roles = SD.ROLE_USER)]
        public async Task<List<string>?> GetUserFollowed()
        {
            var token = Request.Headers["Authorization"].ToString();
            token = token.Remove(0, 7);
            var principal = await AuthControllerUtility.DecodeJWTToken(token);
            var userId = principal.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" && c.Value.Contains("-")).Value;
            if (userId is null)
            {
                return null;
            }

            var userBehavior = await _unitOfWork.UserBehavior.GetFirstOrDefault(userId);

            var returnList = new List<string>();

            if (userBehavior.FollowedUsers.Count() != 0)
            {
                foreach (var user in userBehavior.FollowedUsers)
                {
                    returnList.Add(
                        _db.Users.Where(u => u.Id == user).Select(u => u.UserName).FirstOrDefault()
                        );
                }
            }

            return returnList;
        }
    }
}