namespace Shared.Constants;

public static class UserVerificationMode
{
    public const string VerificationForSignUp = "VERIFICATION_SIGNUP";
    public const string VerificationForSignin = "VERIFICATION_SIGNIN";
    public const string VerificationThisEmail = "VERIFICATION_THIS_EMAIL";
    public const string ForgotPassword = "FORGOT_PASSWORD";
    public const string VerifyCurrentUserEmail = "VERIFY_CURRENT_USER_EMAIL";
    public const string VerifyCurrentUserEmailByLinkOnly = "VERIFY_CURRENT_USER_EMAIL_BY_LINK_ONLY";
    public const string UserInvitation = "USER_INVITATION";
}

public static class VerificationType
{
    public const string SignIn = "sign_in";
    public const string SignUp = "sign_up";
}

public static class UserVerificationStatus
{
    public const int Active = 1;
    public const int Inactive = 0;
}