using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using static System.Net.Mime.MediaTypeNames;

namespace T_Bot
{
    public class UserEntity
    {
        public long Id {  get; private set; }
        public bool IsSearching { get; set; } = false;

        public UserEntity(long id)
        {
            Id = id;
        }

        public static async void SetDbEntity(long Userid)
            {
            await Task.Run(() =>
            {
                using ApplicationContext dbContext = new ApplicationContext();
                {
                    List<UserEntity> users = (from check in dbContext.Users
                                where check.Id! == Userid
                                select check).ToList();
                    if (users.Count == 0)
                    {
                        UserEntity user = new UserEntity(Userid);
                        dbContext.Users.Add(user);
                        dbContext.SaveChanges();
                    }
                }
            });

                
            }
        public static UserEntity GetUserFromDb(long Userid)
        {
            using (ApplicationContext db = new ApplicationContext())
            {
                UserEntity user = (from usr in db.Users
                             where usr.Id == Userid
                             select usr)
                                .First();
                return user;
            }           
        }
        public static async void UpdateUserDB(UserEntity user)
        {
            await Task.Run(() =>
                {
                    using ApplicationContext context = new ApplicationContext();
                    {
                        context.Users.Update(user);
                        context.SaveChanges();
                    }
                });
        }
    }    
}
