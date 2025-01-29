using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace InterviewWizard.Models.User
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [MaxLength(256)]
        public string Salt { get; set; }

        [Required]
        [DefaultValue(SubscriptionType.Free)]
        public SubscriptionType SubscriptionType { get; set; }

        [Required]
        [DefaultValue(false)]
        public bool Verified { get; set; }

        [Required]
        public DateTime Created { get; set; }

        public DateTime? LastLogin { get; set; }

        [DefaultValue(10)]
        public int Credits { get; set; }

        public virtual ICollection<VerifyRequest> VerifyRequests { get; set; }

        static public async Task<string> CreatePasswordHash(string password, string salt)
        {
            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = Encoding.UTF8.GetBytes(salt),
                DegreeOfParallelism = 8,
                MemorySize = 8192,
                Iterations = 4
            };
            var hashBytes = await argon2.GetBytesAsync(32);
            return Convert.ToBase64String(hashBytes);
        }

        static public string CreateSalt()
        {
            byte[] salt = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return Convert.ToBase64String(salt);
        }

        public async Task<bool> VerifyPassword(string password)
        {
            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = Encoding.UTF8.GetBytes(Salt),
                DegreeOfParallelism = 8,
                MemorySize = 8192,
                Iterations = 4
            };
            var hashBytes = await argon2.GetBytesAsync(32);
            return Convert.ToBase64String(hashBytes) == Password;
        }

    }
}
