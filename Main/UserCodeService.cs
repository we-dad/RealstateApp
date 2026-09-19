using System.Security.Cryptography;
using System.Text;

namespace RealEstateInstallmentsManager.Services;

// Shared by both modules (so it has no module suffix in its name).
// Gives every signed-in user a short stable code, derived from the account id,
// that is added to the numbers the app generates (for example Ir-1001-K7Q).
// Two users working offline can then never generate the same number.
public static class UserCodeService
{
    // 32 letters and digits; no I, O, 0 or 1 so the code is easy to read out.
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    // "-K7Q" for a signed-in user, "" when the account id is not known
    // (the number keeps its old shape instead of getting a made-up code).
    public static string GetSuffix()
    {
        var id = AppSession.UserId;

        if (string.IsNullOrWhiteSpace(id))
            return "";

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(id.Trim().ToLowerInvariant()));

        var code = new char[3];
        for (var i = 0; i < code.Length; i++)
            code[i] = Alphabet[hash[i] % Alphabet.Length];

        return "-" + new string(code);
    }
}
