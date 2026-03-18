namespace Application.Configurations;

public class VerificationSettings
{
    public Email Email { get; set; } = new();
    public Phone Phone { get; set; } = new();
}

public class Email
{
    public TimeForVerification TimeForVerification { get; set; } = new();
    public TimeForVerificationThisEmail TimeForVerificationThisEmail { get; set; } = new();
    public TimeForSignIn TimeForSignIn { get; set; } = new();
    public TimeForSignUp TimeForSignUp { get; set; } = new();
    public ForgotPassword ForgotPassword { get; set; } = new();
}

public class Phone
{
    public TimeForSignIn TimeForSignIn { get; set; } = new();
    public TimeForSignUp TimeForSignUp { get; set; } = new();
}

public class TimeForVerification
{
    public int ExpireTimeCode { get; set; }
    public int ExpireTimeToken { get; set; }
    public int ExpireTimeRecord { get; set; }
}

public class TimeForVerificationThisEmail
{
    public int ExpireTimeCode { get; set; }
    public int ExpireTimeToken { get; set; }
    public int ExpireTimeRecord { get; set; }
}

public class TimeForSignIn
{
    public int ExpireTimeCode { get; set; }
    public int ExpireTimeToken { get; set; }
    public int ExpireTimeRecord { get; set; }
}

public class TimeForSignUp
{
    public int ExpireTimeCode { get; set; }
    public int ExpireTimeToken { get; set; }
    public int ExpireTimeRecord { get; set; }
}

public class ForgotPassword
{
    public int ExpireTimeCode { get; set; }
    public int ExpireTimeToken { get; set; }
    public int ExpireTimeRecord { get; set; }
}