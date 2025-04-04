using HashidsNet;

namespace ConoPizzaWeb.Services
{
    public class SecureIdService
    {
        private readonly IHashids _hashids;

        public SecureIdService(string salt = null, int minHashLength = 8)
        {
            // Use a strong salt - ideally from configuration
            string secureSalt = salt ?? "YourSecureSaltForConoPizza123";
            _hashids = new Hashids(secureSalt, minHashLength);
        }

        public string Encode(int id)
        {
            return _hashids.Encode(id);
        }

        public int? Decode(string hash)
        {
            try
            {
                var numbers = _hashids.Decode(hash);
                return numbers.Length > 0 ? numbers[0] : null;
            }
            catch
            {
                return null;
            }
        }
    }
}