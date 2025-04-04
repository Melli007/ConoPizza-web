using ConoPizzaWeb.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace ConoPizzaWeb.Models.Feedback
{
    public class Feedback : BaseEntity
    {
        public int FeedbackId { get; set; }

        [Required, MaxLength(100)]
        public string Emri { get; set; }  // 'CustomerName' if you want a clearer name

        [Required, EmailAddress, MaxLength(255)]
        public string Email { get; set; }

        [Required, MaxLength(200)]
        public string Subject { get; set; }

        [Required, MaxLength(1000)]
        public string Mesazhi { get; set; }  // 'Message'

        [Range(1, 5)]
        public int RatingStars { get; set; }
    }
}
