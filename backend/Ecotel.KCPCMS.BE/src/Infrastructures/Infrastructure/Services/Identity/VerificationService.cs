using System.Web;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Configurations;
using Application.Dto.Authorization.Verification;
using Application.Interfaces.Infrastructures.Integrates.External.Service.Email;
using Application.Interfaces.Services;
using Application.Utility;
using Domain.Entities.Identity;
using Humanizer;
using Microsoft.Extensions.Options;
using Shared.Constants;
using Shared.Constants.EmailTemplate;
using SendVerificationByEmailInput = Application.Dto.Authorization.Verification.SendVerificationByEmailInput;

namespace Infrastructure.Services.Identity;

public class VerificationService(
    IUnitOfWork unitOfWork,
    IOptions<AppSettings> appSettings,
    IOptions<VerificationSettings> verificationSettings,
    IEmailService emailService
    ) : IVerificationService
{
    private readonly AppSettings _appSettings = appSettings.Value;
    private readonly VerificationSettings _verificationSettings = verificationSettings.Value;
    private readonly IWriteRepository<User> _userRepository = unitOfWork.GetRepository<User>();
    private readonly IWriteRepository<UserVerification> _userVerificationRepository = unitOfWork.GetRepository<UserVerification>();

    public async Task<SendVerificationEmailOutputModel> SaveAndSendVerificationByEmail(SendVerificationEmailModel input, int? userId = null)
    {
        // trigger will delete the records has DeletedDate > CurrentDate
        // Link has disable on system Except the Forgot password

        input.Locale = this.NormalizeLocale(input.Locale);

        var userVerification = await this.PrepareDataUserVerification(input.Email, input.Mode, userId);

        var userInfo = await _userRepository.GetFirstOrDefaultAsync(predicate: u => u.NormalizedEmail == input.Email.ToUpperInvariant());

        switch (input.Mode)
        {
            case UserVerificationMode.VerificationForSignin:
                {
                    userVerification.DeletedDate =
                        DateTime.UtcNow.AddDays(1);
                    userVerification.CodeExpirationDate =
                        DateTime.UtcNow.AddMinutes(30);
                    userVerification.Mode = UserVerificationMode.VerificationForSignin;
                    break;
                }
            case UserVerificationMode.VerificationForSignUp:
                {
                    userVerification.DeletedDate =
                        DateTime.UtcNow.AddDays(1);
                    userVerification.CodeExpirationDate =
                        DateTime.UtcNow.AddMinutes(30);

                    userVerification.Mode = UserVerificationMode.VerificationForSignUp;
                    //deliverData = new
                    //{
                    //    locale = input.Locale,
                    //    mode = UserVerificationMode.VerificationForSignUp,
                    //    code = userVerification.VerificationCode,
                    //    email = input.Email,
                    //    username = userInfo?.FirstName
                    //};
                    break;
                }
            case UserVerificationMode.ForgotPassword:
                {
                    string token = Guid.NewGuid().ToString("N").Truncate(328);

                    int codeForgotExpirationMinutes = _verificationSettings.Email.ForgotPassword.ExpireTimeCode;
                    int tokenForgotExpirationMinutes = _verificationSettings.Email.ForgotPassword.ExpireTimeToken;
                    int recordForgotExpirationMinutes = _verificationSettings.Email.ForgotPassword.ExpireTimeRecord;

                    userVerification.CodeExpirationDate = DateTime.UtcNow.AddMinutes(codeForgotExpirationMinutes);
                    userVerification.Mode = UserVerificationMode.ForgotPassword;
                    userVerification.Token = token;
                    userVerification.TokenExpirationDate = DateTime.UtcNow.AddMinutes(tokenForgotExpirationMinutes);
                    userVerification.DeletedDate = DateTime.UtcNow.AddDays(recordForgotExpirationMinutes);

                    userVerification.Link = NormalizeUrlToken(EncryptToken(input.Email, token, UserVerificationMode.ForgotPassword), UserVerificationMode.ForgotPassword, input.Email);

                    var deliverData = new SendVerificationByEmailInput()
                    {
                        Locale = input.Locale,
                        Mode = UserVerificationMode.ForgotPassword,
                        Code = userVerification.VerificationCode,
                        Token = token,
                        Link = userVerification.Link,
                        Email = input.Email,
                        UserName = userInfo?.UserName ?? string.Empty,
                    };

                    await emailService.SendVerificationPasswordReset(deliverData, deliverData.Locale);

                    break;
                }
            case UserVerificationMode.VerifyCurrentUserEmail:
                {
                    string token = Guid.NewGuid().ToString("N").Truncate(328);

                    userVerification.Mode = UserVerificationMode.VerifyCurrentUserEmail;
                    userVerification.Token = token;
                    userVerification.DeletedDate =
                        DateTime.UtcNow.AddDays(_verificationSettings.Email.TimeForVerification.ExpireTimeRecord);
                    userVerification.CodeExpirationDate =
                        DateTime.UtcNow.AddDays(_verificationSettings.Email.TimeForVerification.ExpireTimeCode);

                    userVerification.Link =
                        NormalizeUrlToken(EncryptToken(input.Email, token,
                                UserVerificationMode.VerifyCurrentUserEmail),
                            UserVerificationMode.VerifyCurrentUserEmail, input.Email);

                    var deliverData = new SendVerificationByEmailInput
                    {
                        Locale = input.Locale,
                        Mode = UserVerificationMode.VerifyCurrentUserEmail,
                        Code = userVerification.VerificationCode,
                        Token = token,
                        Link = userVerification.Link,
                        Email = input.Email,
                        UserName = userInfo.UserName
                    };
                    await emailService.SendVerificationEmailVerify(deliverData, deliverData.Locale);

                    break;
                }
            case UserVerificationMode.VerifyCurrentUserEmailByLinkOnly:
                {
                    userVerification.DeletedDate =
                        DateTime.UtcNow.AddDays(_verificationSettings.Email.TimeForVerification.ExpireTimeRecord);
                    userVerification.CodeExpirationDate =
                        DateTime.UtcNow.AddDays(_verificationSettings.Email.TimeForVerification.ExpireTimeCode);

                    string token = Guid.NewGuid().ToString("N").Truncate(328);

                    userVerification.Mode = UserVerificationMode.VerifyCurrentUserEmailByLinkOnly;
                    userVerification.Token = token;

                    userVerification.TokenExpirationDate = DateTime.UtcNow.AddDays(360);
                    userVerification.Link = NormalizeUrlToken(EncryptToken(input.Email, token, UserVerificationMode.VerifyCurrentUserEmailByLinkOnly), UserVerificationMode.VerifyCurrentUserEmailByLinkOnly, input.Email);

                    var deliverData = new SendVerificationByEmailInput
                    {
                        Locale = input.Locale,
                        Mode = UserVerificationMode.VerifyCurrentUserEmailByLinkOnly,
                        Code = userVerification.VerificationCode,
                        Token = token,
                        Link = userVerification.Link,
                        Email = input.Email
                        //UserName = userInfo!.Fullname
                    };
                    await emailService.SendVerificationEmailVerifyLinkOnly(deliverData, input.Locale);
                    break;
                }
            case UserVerificationMode.VerificationThisEmail:
                {
                    userVerification.DeletedDate =
                        DateTime.UtcNow.AddDays(_verificationSettings.Email.TimeForVerificationThisEmail.ExpireTimeRecord);
                    userVerification.CodeExpirationDate =
                        DateTime.UtcNow.AddDays(_verificationSettings.Email.TimeForVerificationThisEmail.ExpireTimeCode);

                    userVerification.Mode = UserVerificationMode.VerificationThisEmail;

                    break;
                }
        }

        if (userVerification.Id > 0)
        {
            _userVerificationRepository.Update(userVerification);
        }
        else
        {
            await _userVerificationRepository.InsertAsync(userVerification);
        }

        await unitOfWork.SaveChangesAsync();

        return new SendVerificationEmailOutputModel
        {
            Email = input.Email,
            Message = "",
            Sent = true,
            UserId = userId
        };
    }

    public async Task<EmailConfirmVerificationOutput> VerifyUserEmailByCode(EmailVerificationModel input, string mode)
    {
        if (string.IsNullOrEmpty(input.Code))
        {
            return new EmailConfirmVerificationOutput
            {
                VerifiedEmail = false,
                Email = "",
                Message = CustomResponseMessage.InvalidParams,
            };
        }

        var verificationCode = await _userVerificationRepository.GetFirstOrDefaultAsync(predicate: v => v.Email == input.Email && v.VerificationCode == input.Code);

        if (verificationCode == null)
        {
            return new EmailConfirmVerificationOutput
            {
                VerifiedEmail = false,
                Email = "",
                Message = CustomResponseMessage.InvalidEmailOrCode,
            };
        }

        if (mode != verificationCode.Mode &&
            mode != UserVerificationMode.VerifyCurrentUserEmail)
        {
            return new EmailConfirmVerificationOutput
            {
                VerifiedEmail = false,
                Email = "",
                Message = CustomResponseMessage.NotAllowed,
            };
        }

        if (verificationCode.CodeExpirationDate < DateTime.UtcNow)
        {
            return new EmailConfirmVerificationOutput
            {
                VerifiedEmail = false,
                Email = "",
                Message = CustomResponseMessage.CodeExpired,
            };
        }

        verificationCode.CodeExpirationDate = DateTime.UtcNow;
        verificationCode.TokenExpirationDate = DateTime.UtcNow;
        verificationCode.Status = UserVerificationStatus.Inactive;
        verificationCode.DeletedDate = DateTime.UtcNow.AddDays(15);

        _userVerificationRepository.Update(verificationCode);
        await unitOfWork.SaveChangesAsync();
        return new EmailConfirmVerificationOutput
        {
            VerifiedEmail = true,
            Email = verificationCode.Email,
            UserId = verificationCode.UserId
        };
    }

    public async Task<EmailConfirmVerificationOutput> VerifyUserEmailByToken(EmailVerificationModel input)
    {
        if (string.IsNullOrEmpty(input.Token))
        {
            return new EmailConfirmVerificationOutput
            {
                VerifiedEmail = false,
                Email = "",
                Message = CustomResponseMessage.InvalidParams,
            };
        }

        var verificationToken = await _userVerificationRepository.GetFirstOrDefaultAsync(predicate: v => v.Email == input.Email && v.Token == input.Token);

        if (verificationToken == null)
        {
            return new EmailConfirmVerificationOutput
            {
                VerifiedEmail = false,
                Email = "",
                Message = CustomResponseMessage.InvalidEmailOrCode,
            };
        }

        if (verificationToken.TokenExpirationDate < DateTime.UtcNow)
        {
            return new EmailConfirmVerificationOutput
            {
                VerifiedEmail = false,
                Email = "",
                Message = CustomResponseMessage.TokenExpired,
            };
        }

        verificationToken.CodeExpirationDate = DateTime.UtcNow;
        verificationToken.TokenExpirationDate = DateTime.UtcNow;
        verificationToken.Status = UserVerificationStatus.Inactive;
        verificationToken.DeletedDate = DateTime.UtcNow.AddDays(15);

        _userVerificationRepository.Update(verificationToken);
        await unitOfWork.SaveChangesAsync();
        return new EmailConfirmVerificationOutput
        {
            VerifiedEmail = true,
            Email = verificationToken.Email,
            UserId = verificationToken.UserId
        };
    }

    public async Task<EmailConfirmVerificationOutput> VerifyCurrentUserEmail(VerifyCurrentUserEmailInput input, string mode)
    {
        EmailConfirmVerificationOutput verificationOutput;

        if (string.IsNullOrEmpty(input.Code) && string.IsNullOrEmpty(input.C))
        {
            throw new Exception(CustomResponseMessage.InvalidParams);
        }

        if (!string.IsNullOrEmpty(input.C))
        {
            var verifyTokenInput = new VerifyUserEmailTokenInput(input.C);
            verifyTokenInput.ResolveParameters();

            if (verifyTokenInput.Mode == UserVerificationMode.VerifyCurrentUserEmail || verifyTokenInput.Mode == UserVerificationMode.VerifyCurrentUserEmailByLinkOnly)
            {
                verificationOutput = await VerifyUserEmailByToken(new EmailVerificationModel
                {
                    Token = verifyTokenInput.Token,
                    Email = verifyTokenInput.Email
                });
            }
            else
            {
                throw new Exception(CustomResponseMessage.NotAllowed);
            }
        }
        else
        {
            verificationOutput = await VerifyUserEmailByCode(new EmailVerificationModel
            {
                Code = input.Code,
                Email = input.Email
            }, mode);
        }

        if (!verificationOutput.VerifiedEmail)
        {
            return verificationOutput;
        }

        if (verificationOutput.UserId == null)
        {
            throw new Exception("Current user does not exist");
        }

        // await _unitOfWorkCatalog.Users.SetVerificationEmail(verificationOutput.UserId.Value);
        return verificationOutput;
    }

    public async Task<bool> HasSendVerificationCurrentUserEmail(string email)
    {
        var userVerificationActive = await _userVerificationRepository.GetFirstOrDefaultAsync(predicate: v => v.Email == email, disableTracking: false);

        return userVerificationActive is { Mode: UserVerificationMode.VerifyCurrentUserEmail };
    }

    public EmailVerificationModel ResolveParameters(string t)
    {
        if (string.IsNullOrEmpty(t))
        {
            throw new Exception("Invalid parameters");
        }

        string parameterDecryptString = Utils.DecryptString(t);

        string[] parameters = parameterDecryptString.Split("|");

        if (parameters.Length != 2)
        {
            throw new Exception("Invalid parameters");
        }

        return new EmailVerificationModel
        {
            Email = parameters[0],
            Token = parameters[1]
        };
    }

    public string EncryptToken(string email, string tokenCode, string mode)
    {
        string token = email + "|" + tokenCode + "|" + mode;

        return Utils.EncryptString(token);
    }

    public string NormalizeUrlToken(string token, string mode, string email)
    {
        string cToken = HttpUtility.UrlEncode(token);
        return mode switch
        {
            UserVerificationMode.VerifyCurrentUserEmail =>
                $"{_appSettings.ClientRootAddress}auth/verify-email?c={cToken}&email={email}",
            UserVerificationMode.VerifyCurrentUserEmailByLinkOnly =>
                $"{_appSettings.ClientRootAddress}auth/verify-email?c={cToken}&email={email}",
            UserVerificationMode.ForgotPassword =>
                $"{_appSettings.ClientRootAddress}auth/reset-password/verify-code?c={cToken}&email={email}",
            _ => throw new Exception("Mode does not support")
        };
    }

    private async Task<UserVerification> PrepareDataUserVerification(string email, string mode, int? userId = null)
    {
        int code = Utils.GetRandomRange(100000, 999999);

        var userVerification = new UserVerification
        {
            Token = string.Empty,
            TokenExpirationDate = null,
            VerificationCode = code.ToString(),
            CreatedDate = DateTime.UtcNow,
            Link = string.Empty,
            UserId = userId,
            Status = UserVerificationStatus.Active,
            Email = email,
            Phone = string.Empty
        };

        // this is case verify user profile email
        if (mode == UserVerificationMode.VerifyCurrentUserEmail)
        {
            if (!userId.HasValue)
            {
                throw new NullReferenceException("This mode need UserId");
            }

            var userVerificationActives = (await _userVerificationRepository
                .GetAllAsync(predicate: v => v.Email == email && v.Mode == UserVerificationMode.VerifyCurrentUserEmail))
                .ToList();

            // filter by UserId
            userVerificationActives = userVerificationActives.Where(x => x.UserId == userId.Value).ToList();

            if (userVerificationActives.Count <= 0)
            {
                return userVerification;
            }

            if (userVerificationActives.Count == 1)
            {
                return userVerificationActives[0];
            }

            var takeOneUserVerification = userVerificationActives.First();

            foreach (var userVerificationActive in
                     userVerificationActives.Where(userVerificationActive => userVerificationActive.Id != takeOneUserVerification.Id))
            {
                userVerificationActive.CodeExpirationDate = DateTime.UtcNow;
                userVerificationActive.Status = UserVerificationStatus.Inactive;
                userVerificationActive.DeletedDate = DateTime.UtcNow.AddDays(15);

                _userVerificationRepository.Update(userVerificationActive);
                await unitOfWork.SaveChangesAsync();
            }
            return takeOneUserVerification;
        }

        // this is other case verify email - still use once session
        var verifications = (await _userVerificationRepository
            .GetAllAsync(predicate: v => v.Email == email && v.Mode == UserVerificationMode.VerifyCurrentUserEmail)).ToList();

        if (verifications.Count <= 0)
        {
            return userVerification;
        }

        foreach (var verification in verifications.Where(x => x.Mode != UserVerificationMode.VerifyCurrentUserEmail))
        {
            verification.CodeExpirationDate = DateTime.UtcNow;
            verification.Status = UserVerificationStatus.Inactive;
            verification.DeletedDate = DateTime.UtcNow.AddDays(15);

            _userVerificationRepository.Update(verification);
            await unitOfWork.SaveChangesAsync();
        }

        return userVerification;
    }

    public string NormalizeLocale(string locale)
    {
        return locale switch
        {
            EmailSupportLanguageConst.English =>
                EmailSupportLanguageConst.English,
            _ => EmailSupportLanguageConst.Vietnamese
        };
    }
}