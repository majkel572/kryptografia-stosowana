using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Persistence.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Persistence
{
    internal class UnspentTransactionOutData : IUnspentTransactionOutData
    {
        private List<UnspentTransactionOutput> _unspentTransactionOutputs;
        private readonly object _unspentTransactionOutputsLock = new object();

        public UnspentTransactionOutData()
        {
            _unspentTransactionOutputs = new List<UnspentTransactionOutput>();
        }

        public List<UnspentTransactionOutput> GetUnspentTxOut()
        {
            return _unspentTransactionOutputs;
        }

        public void UpdateUnspentTransactionOutputs(List<TransactionLib> newTransactions)
        {
            lock (_unspentTransactionOutputsLock)
            {
                // nowe transakcje na unspent outputy zamieniamy
                var newUnspentTxOuts = newTransactions
                    .SelectMany(t => t.TransactionOutputs.Select((txOut, index) =>
                        new UnspentTransactionOutput(t.Id, index, txOut.Address, txOut.Amount)))
                    .ToList();

                // zużyte unspenty wyciagamy
                var consumedTxOuts = newTransactions
                    .SelectMany(t => t.TransactionInputs)
                    .Select(txIn => new UnspentTransactionOutput(txIn.TransactionOutputId, txIn.TransactionOutputIndex, string.Empty, 0))
                    .ToList();

                // bierzemy unspenty zużyte, odejmujemy je od puli unspentów i dodajemy do nich nowe unspenty
                _unspentTransactionOutputs = _unspentTransactionOutputs
                    .Where(uTxO => !consumedTxOuts.Any(consumed =>
                        consumed.TransactionOutputId == uTxO.TransactionOutputId &&
                        consumed.TransactionOutputIndex == uTxO.TransactionOutputIndex))
                    .Concat(newUnspentTxOuts)
                    .ToList();
            }
        }
    }
}
