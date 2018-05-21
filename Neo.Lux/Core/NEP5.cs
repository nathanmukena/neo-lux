﻿using Neo.Lux.Cryptography;
using Neo.Lux.Utils;
using System.Numerics;
using System;

namespace Neo.Lux.Core
{
    public class NEP5
    {
        public readonly byte[] ContractHash;
        private readonly NeoAPI api;

        public NEP5(NeoAPI api, string contractHash) : this(api, NeoAPI.GetScriptHashFromString(contractHash))
        {

        }

        public NEP5(NeoAPI api, UInt160 contractHash) : this(api, contractHash.ToArray())
        {

        }

        public NEP5(NeoAPI api, byte[] contractHash)
        {
            this.api = api;
            this.ContractHash = contractHash;
        }

        public NEP5(NeoAPI api, string contractHash, string name, BigInteger decimals)
            : this(api, contractHash)
        {
            this._decimals = decimals;
            this._name = name;
        }

        private string _name = null;
        public string Name
        {
            get
            {
                InvokeResult response = null;
                try
                {
                    if (_name == null)
                    {
                        response = api.InvokeScript(ContractHash, "name", new object[] { "" });
                        _name = System.Text.Encoding.ASCII.GetString((byte[])response.stack[0]);
                    }

                    return _name;
                }
                catch (Exception e)
                {
                    throw new NeoException("Api did not return a value.", e);
                }

            }
        }

        private string _symbol = null;
        public string Symbol
        {
            get
            {
                InvokeResult response = null;
                try
                {
                    if (_symbol == null)
                    {
                        response = api.InvokeScript(ContractHash, "symbol", new object[] { "" });
                        _symbol = System.Text.Encoding.ASCII.GetString((byte[])response.stack[0]);
                    }

                    return _symbol;
                }
                catch (Exception e)
                {
                    throw new NeoException("Api did not return a value.", e);
                }
            }
        }


        private BigInteger _decimals = -1;
        public BigInteger Decimals
        {
            get
            {
                InvokeResult response = null;
                try
                {
                    if (_decimals < 0)
                    {
                        response = api.InvokeScript(ContractHash, "decimals", new object[] { "" });
                        _decimals = (BigInteger)response.stack[0];
                    }

                    return _decimals;
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                    throw new NeoException("Api did not return a value.", e);
                }

            }
        }

        private BigInteger _totalSupply = -1;
        public BigInteger TotalSupply
        {
            get
            {
                InvokeResult response = null;
                try
                {
                    if (_totalSupply < 0)
                    {
                        response = api.InvokeScript(ContractHash, "totalSupply", new object[] { "" });
                        _totalSupply = new BigInteger((byte[])response.stack[0]);

                        var decs = Decimals;
                        while (decs > 0)
                        {
                            _totalSupply /= 10;
                            decs--;
                        }
                    }

                    return _totalSupply;

                }
                catch (Exception e)
                {
                    throw new NeoException("Api did not return a value.", e);
                }

            }
        }

        // FIXME - I'm almost sure that this code won't return non-integer balances correctly...
        private decimal ConvertToDecimal(BigInteger value)
        {
            var decs = this.Decimals;
            while (decs > 0)
            {
                value /= 10;
                decs--;
            }
            return (decimal)value;
        }

        private BigInteger ConvertToBigInt(decimal value)
        {
            var decs = this.Decimals;
            while (decs > 0)
            {
                value *= 10;
                decs--;
            }
            return new BigInteger((ulong)value);
        }

        public decimal BalanceOf(string address)
        {
            return BalanceOf(address.GetScriptHashFromAddress());
        }

        public decimal BalanceOf(KeyPair keys)
        {
            return BalanceOf(keys.address);
        }

        public decimal BalanceOf(byte[] addressHash)
        {
            InvokeResult response = new InvokeResult();
            try
            {
                response = api.InvokeScript(ContractHash, "balanceOf", new object[] { addressHash });
                var balance = new BigInteger((byte[])response.stack[0]);
                return ConvertToDecimal(balance);
            }
            catch
            {
                throw new NeoException("Api did not return a value." + response);
            }
        }

        public InvokeResult Transfer(KeyPair from_key, string to_address, decimal value)
        {
            return Transfer(from_key, to_address.GetScriptHashFromAddress(), value);
        }

        public InvokeResult Transfer(KeyPair from_key, byte[] to_address_hash, decimal value)
        {
            Console.WriteLine("NEP5 token " + Name + " transfer " + value);
            BigInteger amount = ConvertToBigInt(value);

            var sender_address_hash = from_key.address.GetScriptHashFromAddress();
            var response = api.CallContract(from_key, ContractHash, "transfer", new object[] { sender_address_hash, to_address_hash, amount });
            return response;
        }

        // optional methods, not all NEP5 support this!

        public decimal Allowance(string from_address, string to_address)
        {
            return Allowance(from_address.GetScriptHashFromAddress(), to_address.GetScriptHashFromAddress());

        }

        public decimal Allowance(byte[] from_address_hash, byte[] to_address_hash)
        {
            var response = api.InvokeScript(ContractHash, "allowance", new object[] { from_address_hash, to_address_hash });

            try
            {
                return ConvertToDecimal((BigInteger)response.stack[0]);
            }
            catch (Exception e)
            {
                throw new NeoException("Api did not return a value.", e);
            }
        }

        public Transaction TransferFrom(byte[] originator, byte[] from, byte[] to, BigInteger amount)
        {
            throw new System.NotImplementedException();
        }

        public Transaction Approve(byte[] originator, byte[] to, BigInteger amount)
        {
            throw new System.NotImplementedException();
        }

        public InvokeResult Deploy(KeyPair owner_key)
        {
            var response = api.CallContract(owner_key, ContractHash, "deploy", new object[] { });
            return response;
        }

    }
}
