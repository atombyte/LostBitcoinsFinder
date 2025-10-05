using Newtonsoft.Json.Linq;
using QRCoder;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using static NBitcoin.Scripting.OutputDescriptor;


class LostBitcoinsFinder
{
    private const string Base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
    private static readonly byte[] AlphabetIndex = Enumerable.Repeat((byte)255, 128).ToArray();
    private static readonly object FileLock = new object();

    static LostBitcoinsFinder()
    {
        for (int i = 0; i < Base58Alphabet.Length; i++) AlphabetIndex[Base58Alphabet[i]] = (byte)i;
    }

    static void Main(string[] args)
    {


        int numCores = Environment.ProcessorCount;
        bool visualize = false;

        foreach (var arg in args)
        {
            if (int.TryParse(arg, out var n) && n > 0)
            {
                numCores = Math.Min(n, Environment.ProcessorCount);
            }
            else if (arg.Equals("v", StringComparison.OrdinalIgnoreCase))
            {
                visualize = true;
            }
        }

        //numCores = 1;
        Parallel.For(0, numCores, _ => RunWorker(visualize));
    }

    static void RunWorker(bool visualize)
    {
        var rng = RandomNumberGenerator.Create();
        var buffer = new char[52];
        int i = 1;
        int step = 10000000;

        string rpcUrl = "http://umbrel.local:8332/"; // Adjust to your Bitcoin Core RPC URL
        string rpcUser = "your_rpc_username"; // Adjust to your RPC username
        string rpcPass = "your_rpc_password"; // Adjust to your RPC password


		var rnd = new Random();
		string[] prefixes = { "5J", "5K", "5H", "L1", "L2", "Kx", "Kz" };

        while (true)
        {
            FillRandomBase58(rng, buffer);

            string prefix = prefixes[rnd.Next(prefixes.Length)];

			int totalLength = prefix.StartsWith("5") ? 51 : 52;
			string candidate = prefix + new string(buffer, 0, totalLength - prefix.Length);


            if (IsValidWif(candidate, out var pkHex, out bool compressed, out byte version))
            {

                using (var gen = new QRCodeGenerator())
                using (var data = gen.CreateQrCode(candidate, QRCodeGenerator.ECCLevel.H))
                {
                    string qr = new AsciiQRCode(data).GetGraphic(1, "  ", "██");

                    Console.WriteLine("\n");
                    Console.WriteLine(qr);

                    var addresses = WifAddressHelper.DeriveAllFromWif(candidate);

                    decimal total_balance = 0;
                    foreach (var a in addresses)
                    {
                        foreach (var addr in new[] { a.P2PKH, a.P2SH_P2WPKH, a.P2WPKH })
                        {
                            var utxos = RpcScanner.ScanAddresses(new[] { addr }, rpcUrl, rpcUser: rpcUser, rpcPass: rpcPass);
                            decimal balance = 0;
                            foreach (var u in utxos) balance += u.Amount;

                            if (balance > 0)
                            {
                                total_balance += balance;
                                Console.WriteLine($"{addr} | Balance: {balance} BTC");
                            }
                        }
                    }

                    var line = $"{candidate} | Balance: {total_balance} BTC";

                    Console.WriteLine(line);
                    Console.WriteLine("\n");


                    lock (FileLock)
                    {
                        File.AppendAllText("found.txt", line + Environment.NewLine);
                    }

                }

            }
            else
            {
                if (visualize == true) Console.WriteLine(candidate);
            }


			if (i % step == 1)
			{
				i = 1;
				Console.Write("°");
			}
			
            i++;
        }
    }


    static void FillRandomBase58(RandomNumberGenerator rng, char[] buf)
    {
        var bytes = new byte[buf.Length];
        rng.GetBytes(bytes);
        for (int i = 0; i < buf.Length; i++) buf[i] = Base58Alphabet[bytes[i] % Base58Alphabet.Length];
    }

    static bool IsValidWif(string wif, out string privateKeyHex, out bool isCompressed, out byte version)
    {
        privateKeyHex = null; isCompressed = false; version = 0;
        if (string.IsNullOrEmpty(wif)) return false;

        byte[] decoded;
        try { decoded = Base58Decode(wif); } catch { return false; }
        if (decoded.Length < 5) return false;

        int payloadLen = decoded.Length - 4;
        var payload = new byte[payloadLen];
        Array.Copy(decoded, 0, payload, 0, payloadLen);
        var checksum = new byte[4];
        Array.Copy(decoded, payloadLen, checksum, 0, 4);

        var hash = DoubleSha256(payload);
        for (int i = 0; i < 4; i++) if (checksum[i] != hash[i]) return false;

        if (payloadLen != 33 && payloadLen != 34) return false;
        version = payload[0];

        var key = new byte[32];
        Array.Copy(payload, 1, key, 0, 32);
        if (payloadLen == 34)
        {
            if (payload[33] != 0x01) return false;
            isCompressed = true;
        }
        if (key.All(b => b == 0x00)) return false;

        privateKeyHex = BitConverter.ToString(key).Replace("-", "").ToLowerInvariant();
        return true;
    }

    static byte[] DoubleSha256(byte[] data)
    {
        using var sha = SHA256.Create();
        var h1 = sha.ComputeHash(data);
        return sha.ComputeHash(h1);
    }

    static byte[] Base58Decode(string s)
    {
        if (string.IsNullOrEmpty(s)) return Array.Empty<byte>();
        int zeros = 0; while (zeros < s.Length && s[zeros] == '1') zeros++;

        var b256 = new byte[s.Length];
        for (int i = 0; i < s.Length; i++)
        {
            int carry = s[i] < 128 ? AlphabetIndex[s[i]] : (byte)255;
            if (carry == 255) throw new FormatException("Invalid Base58 character");
            for (int j = b256.Length - 1; j >= 0; j--)
            {
                int val = b256[j] * 58 + carry;
                b256[j] = (byte)(val & 0xFF);
                carry = val >> 8;
            }
            if (carry != 0) throw new FormatException("Base58 carry overflow");
        }

        int idx = 0; while (idx < b256.Length && b256[idx] == 0) idx++;
        var result = new byte[zeros + (b256.Length - idx)];
        for (int i = 0; i < zeros; i++) result[i] = 0x00;
        Array.Copy(b256, idx, result, zeros, b256.Length - idx);
        return result;
    }
}