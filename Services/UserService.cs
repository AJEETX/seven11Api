using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using WebApi.Helpers;
using WebApi.Identity;
using WebApi.Model;

namespace WebApi.Services
{
    public interface IUserService
    {
        User Create(User user, string password);
        User Authenticate(string username, string password);
        List<User> GetUsers();
        User GetUserById(string id);
        bool UpdateUser(User user);
    }

    public class UserService : IUserService
    {
        private DataContext _context;
        private ITokeniser _tokeniser;

        public UserService(IOptions<Settings> settings, ITokeniser tokeniser)
        {
            _context = new DataContext(settings);
            _tokeniser = tokeniser;
        }
        public User Create(User user, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new AppException("Password is required");

            if (_context.Users.Find(u => u.Username == user.Username.Trim()).FirstOrDefault()!=null)
                throw new AppException("Username '" + user.Username + "' is already taken");
            try
            {    
                byte[] passwordHash, passwordSalt;
                PasswordHasher.CreatePasswordHash(password, out passwordHash, out passwordSalt);

                user.Roles=new List<Role>{new Role{Name= "Admin" }};
                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;
                user.Username=user.Username.Trim();
                user.UserId=_tokeniser.CreateToken(user.FirstName,user.LastName);
                _context.Users.InsertOne(user);
            }
            catch (AppException)
            {
                //shout/catch/throw/log
            }
            return user;
        }
        
        public User GetUserById(string id)
        {
            var user=default(User);
            if(string.IsNullOrWhiteSpace(id)) return user;
            try{
                user= _context.Users.Find(u=>u.ID==GetInternalId(id) || u.UserId==id )?.FirstOrDefault();
            }
            catch (AppException)
            {
                //shout/catch/throw/log
            }        
            return user;    
        }
        public User Authenticate(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                throw new AppException("No username/password");
            try
            {    
            
                var user = _context.Users.Find(x => x.Username == username).FirstOrDefault();

                if (user == null)
                    throw new AppException("No user found");


                if (!PasswordHasher.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                    throw new AppException("Password incorrect");
                return user;
            }
            catch (AppException)
            {
               return null; //shout/catch/throw/log
            }            
        }

        public List<User> GetUsers()
        {
            try{
                return _context.Users.Find(_=>true).ToList();
            }
            catch (AppException)
            {
               return null; //shout/catch/throw/log
            }               
        }

        public bool UpdateUser(User user)
        {
            try{
                var filter = Builders<User>.Filter.Eq(s => s.Username, user.Username);
                var update = Builders<User>.Update.Set(s => s.FirstName, user.FirstName)
                .Set(s => s.LastName, user.LastName)
                .Set(s => s.Location, user.Location);            
                var updateResult = _context.Users.UpdateOne(filter,update);
                return updateResult.IsAcknowledged && updateResult.MatchedCount>0;                
            }
            catch (AppException)
            {
               return false; //shout/catch/throw/log
            }               
        }
                // Try to convert the Id to a BSonId value
        private ObjectId GetInternalId(string id)
        {
            if (!ObjectId.TryParse(id, out ObjectId internalId))
                internalId = ObjectId.Empty;

            return internalId;
        }
    }
}