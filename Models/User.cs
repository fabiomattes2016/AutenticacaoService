using System.ComponentModel.DataAnnotations;
public class User : IValidatableObject
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Campo nome é obrigatório")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Campo nome de usuário é obrigatório")]
    public string? Username { get; set; }

    [Required(ErrorMessage = "Campo senha é obrigatório"), MinLength(8, ErrorMessage = "A senha deve conter 8 caracteres ou mais")]
    public string? Password { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public override bool Equals(object? obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string? ToString()
    {
        return base.ToString();
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.Equals(Name, "User", StringComparison.OrdinalIgnoreCase))
        {
            yield return new("Campo nome é obrigatório");
        }

        if (string.Equals(Username, "User", StringComparison.Ordinal))
        {
            yield return new("Campo usuário é obrigatório");
        }

        if (string.Equals(Password, "User", StringComparison.OrdinalIgnoreCase))
        {
            yield return new("Campo senha é obrigatório");
        }

        if (DateTime.Equals(CreatedAt, "User"))
        {
            yield return new("Campo data de cadastro é obrigatório");
        }
    }
}