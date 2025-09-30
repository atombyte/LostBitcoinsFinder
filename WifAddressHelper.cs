using NBitcoin;
using System;
using System.Collections.Generic;
using System.Net;


public static class WifAddressHelper
{
    public class DerivedAddresses
    {
        public bool Compressed { get; set; }
        public string P2PKH { get; set; }
        public string P2SH_P2WPKH { get; set; }
        public string P2WPKH { get; set; }
    }

    public static List<DerivedAddresses> DeriveAllFromWif(string wif)
    {
        if (string.IsNullOrWhiteSpace(wif)) throw new ArgumentException("WIF empty", nameof(wif));

        BitcoinSecret secret = null;
        Network network = null;

        var results = new List<DerivedAddresses>();


        try { secret = new BitcoinSecret(wif, Network.Main); network = Network.Main; }
        catch
        {
            return results; // invalid WIF
        }

        

        // compressed pubkey
        var compKey = secret.PrivateKey; // default NBitcoin parses as compressed if WIF has suffix
        var compPub = compKey.PubKey.Compress();
        results.Add(new DerivedAddresses
        {
            Compressed = true,
            P2PKH = compPub.GetAddress(ScriptPubKeyType.Legacy, network).ToString(),
            P2SH_P2WPKH = compPub.GetAddress(ScriptPubKeyType.SegwitP2SH, network).ToString(),
            P2WPKH = compPub.GetAddress(ScriptPubKeyType.Segwit, network).ToString()
        });

        // uncompressed pubkey
        var uncompPub = compKey.PubKey.Decompress();
        results.Add(new DerivedAddresses
        {
            Compressed = false,
            P2PKH = uncompPub.GetAddress(ScriptPubKeyType.Legacy, network).ToString(),
            P2SH_P2WPKH = uncompPub.GetAddress(ScriptPubKeyType.SegwitP2SH, network).ToString(),
            P2WPKH = uncompPub.GetAddress(ScriptPubKeyType.Segwit, network).ToString()
        });

        return results;
    }
}

