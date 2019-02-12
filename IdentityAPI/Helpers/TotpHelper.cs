using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace IdentityAPI.Helpers
{
    public interface ITotpHelper
    {
        string GenerateRandomSecret();
        string GenerateSecretUrl(string secret, string email);
        bool ConfirmSecret(string otp, string key);
    }

    public class TotpHelper : ITotpHelper
    {
        private byte[] FromBase32String(string base32String)
        {
            const int InByteSize = 8;
            const int OutByteSize = 5;
            const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

            // Check if string is null
            if (base32String == null)
            {
                return null;
            }
            // Check if empty
            else if (base32String == string.Empty)
            {
                return new byte[0];
            }

            // Convert to upper-case
            string base32StringUpperCase = base32String.ToUpperInvariant();

            // Prepare output byte array
            byte[] outputBytes = new byte[base32StringUpperCase.Length * OutByteSize / InByteSize];

            // Check the size
            if (outputBytes.Length == 0)
            {
                throw new ArgumentException("Specified string is not valid Base32 format because it doesn't have enough data to construct a complete byte array");
            }

            // Position in the string
            int base32Position = 0;

            // Offset inside the character in the string
            int base32SubPosition = 0;

            // Position within outputBytes array
            int outputBytePosition = 0;

            // The number of bits filled in the current output byte
            int outputByteSubPosition = 0;

            // Normally we would iterate on the input array but in this case we actually iterate on the output array
            // We do it because output array doesn't have overflow bits, while input does and it will cause output array overflow if we don't stop in time
            while (outputBytePosition < outputBytes.Length)
            {
                // Look up current character in the dictionary to convert it to byte
                int currentBase32Byte = Base32Alphabet.IndexOf(base32StringUpperCase[base32Position]);

                // Check if found
                if (currentBase32Byte < 0)
                {
                    throw new ArgumentException(string.Format("Specified string is not valid Base32 format because character \"{0}\" does not exist in Base32 alphabet", base32String[base32Position]));
                }

                // Calculate the number of bits we can extract out of current input character to fill missing bits in the output byte
                int bitsAvailableInByte = Math.Min(OutByteSize - base32SubPosition, InByteSize - outputByteSubPosition);

                // Make space in the output byte
                outputBytes[outputBytePosition] <<= bitsAvailableInByte;

                // Extract the part of the input character and move it to the output byte
                outputBytes[outputBytePosition] |= (byte)(currentBase32Byte >> (OutByteSize - (base32SubPosition + bitsAvailableInByte)));

                // Update current sub-byte position
                outputByteSubPosition += bitsAvailableInByte;

                // Check overflow
                if (outputByteSubPosition >= InByteSize)
                {
                    // Move to the next byte
                    outputBytePosition++;
                    outputByteSubPosition = 0;
                }

                // Update current base32 byte completion
                base32SubPosition += bitsAvailableInByte;

                // Check overflow or end of input array
                if (base32SubPosition >= OutByteSize)
                {
                    // Move to the next character
                    base32Position++;
                    base32SubPosition = 0;
                }
            }

            return outputBytes;
        }

        private string GenerateHash(string key, long timeStep)
        {
            string result = null;

            // Get the key
            // Key is 16-character base32-encoded shared secret
            var keyBytes = FromBase32String(key);
            // JBSWY3DPEHPK3PXP
            //new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o', (byte)'!', 0xDE, 0xAD, 0xBE, 0xEF };
            //new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x21, 0xDE, 0xAD, 0xBE, 0xEF };

            using (var hmacsha1 = new HMACSHA1(keyBytes))
            {
                // Get the counter
                var counter = BitConverter.GetBytes(timeStep);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(counter);
                }

                // Calculate the hash
                byte[] hashedBytes = hmacsha1.ComputeHash(counter);

                // Get the bytes using dynamic truncation
                int pos = hashedBytes[hashedBytes.Length - 1] & 0x0F;

                int hash = ((hashedBytes[pos] & 0x7F) << 24) |
                             ((hashedBytes[pos + 1] & 0xFF) << 16) |
                             ((hashedBytes[pos + 2] & 0xFF) << 8) |
                             (hashedBytes[pos + 3] & 0xFF);

                // Use 6 digits
                hash = hash % (int)Math.Pow(10, 6);

                // Convert to string
                result = hash.ToString();
            }

            return result;
        }

        public bool ConfirmSecret(string otp, string key)
        {
            // Get the time step
            long timeStep = DateTimeOffset.Now.ToUnixTimeSeconds() / 30;

            // Check with submitted otp
            return otp.Equals(GenerateHash(key, timeStep - 1))
                || otp.Equals(GenerateHash(key, timeStep))
                || otp.Equals(GenerateHash(key, timeStep + 1));
        }

        public string GenerateRandomSecret()
        {
            Random random = new Random();
            const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            const int SecretSize = 16;
            string result = "";

            for (int i = 0; i < SecretSize; i++)
            {
                result += Base32Alphabet[random.Next(Base32Alphabet.Length)].ToString();
            }

            return result;
        }

        public string GenerateSecretUrl(string secret, string email)
        {
            return @"otpauth://totp/roman015.com:"
                    + email
                    + "?secret="
                    + secret
                    + "&issuer=roman015.com&algorithm=SHA1&digits=6&period=30";
        }
    }
}
