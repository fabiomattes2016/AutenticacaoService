using Flunt.Notifications;
using Flunt.Validations;

namespace AutenticacaoService.ViewModels
{
    public class CreateUserViewModel : Notifiable<Notification>
    {
        public string? Name { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public User MapTo()
        {
            AddNotifications(new Contract<Notification>()
            .Requires()
            .IsNotNullOrEmpty(Name, "Informe o nome do usuário")
            .IsNotNullOrEmpty(Username, "Informe o usuário")
            .IsNotNullOrEmpty(Password, "Informe a senha")
            .IsNotNull(CreatedAt, "Informe a data da criação do registro")
            );



            return new User
            {
                Name = Name,
                Username = Username,
                Password = Password,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            };
        }
    }
}