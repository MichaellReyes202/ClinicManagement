using System.Security.Cryptography;
using System.Text;

namespace Application.Util;

public static class PasswordGenerator
{
    private static readonly string UppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private static readonly string LowercaseChars = "abcdefghijklmnopqrstuvwxyz";
    private static readonly string DigitChars = "0123456789";
    private static readonly string SpecialChars = "!@#$%^&*()-+"; // Caracteres especiales comunes

    public static string GenerateTemporaryPassword(int length = 8)
    {
        // Define todos los caracteres posibles
        string allChars = UppercaseChars + LowercaseChars + DigitChars + SpecialChars;

        // Usa un generador de números aleatorios criptográficamente seguro (preferido)
        var password = new StringBuilder();

        // 1. Asegurar la complejidad: Añadir al menos un carácter de cada tipo
        password.Append(GetRandomChar(UppercaseChars));
        password.Append(GetRandomChar(LowercaseChars));
        password.Append(GetRandomChar(DigitChars));
        password.Append(GetRandomChar(SpecialChars));

        // 2. Rellenar el resto de la contraseña
        int remainingLength = length - 4; // Ya hemos añadido 4 caracteres de complejidad
        for (int i = 0; i < remainingLength; i++)
        {
            password.Append(GetRandomChar(allChars));
        }

        // 3. Mezclar (shuffle) la contraseña para que el orden de los tipos sea aleatorio
        return Shuffle(password.ToString());
    }

    private static char GetRandomChar(string charSet)
    {
        // Genera un índice aleatorio seguro dentro del rango de la cadena de caracteres
        return charSet[RandomNumberGenerator.GetInt32(charSet.Length)];
    }

    private static string Shuffle(string str)
    {
        // Método para mezclar los caracteres de la cadena
        char[] array = str.ToCharArray();
        int n = array.Length;
        while (n > 1)
        {
            n--;
            int k = RandomNumberGenerator.GetInt32(n + 1);
            (array[n], array[k]) = (array[k], array[n]);
        }
        return new string(array);
    }
}


