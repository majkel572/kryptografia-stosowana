using BlockChainP2P.WalletHandler.KeyManagement;
using BlockChainP2P.WalletHandler.WalletManagement;
using FluentAssertions;

namespace BlockChainP2P.WalletHandler.Test
{
    public class WalletTests
    {
        private Wallet _wallet;

        public WalletTests()
        {
            _wallet = new Wallet();
        }

        [Fact]
        public void Should_Add_KeyPair_To_Wallet()
        {
            // Arrange
            var keyPair = KeyGenerator.GenerateKeys();

            // Act
            _wallet.AddKeyPair(keyPair);

            // Assert
            var publicAddresses = _wallet.GetPublicAddresses();
            publicAddresses.Should().Contain(keyPair.PublicKey);
        }

        [Fact]
        public void Should_Remove_KeyPair_From_Wallet()
        {
            // Arrange
            var keyPair1 = KeyGenerator.GenerateKeys();
            var keyPair2 = KeyGenerator.GenerateKeys();
            _wallet.AddKeyPair(keyPair1);
            _wallet.AddKeyPair(keyPair2);

            // Act
            _wallet.RemoveKeyPair(keyPair1);

            // Assert
            var publicAddresses = _wallet.GetPublicAddresses();
            publicAddresses.Should().NotContain(keyPair1.PublicKey);
            publicAddresses.Should().Contain(keyPair2.PublicKey);
        }

        [Fact]
        public void Should_Switch_Active_KeyPair_In_Wallet()
        {
            // Arrange
            var keyPair1 = KeyGenerator.GenerateKeys();
            var keyPair2 = KeyGenerator.GenerateKeys();
            _wallet.AddKeyPair(keyPair1);
            _wallet.AddKeyPair(keyPair2);

            // Act
            _wallet.SetActiveKeyPair(1);

            // Assert
            _wallet.GetActivePublicAddress().Should().Be(keyPair2.PublicKey);
        }

        [Fact]
        public void Should_Sign_Transaction_With_Active_KeyPair()
        {
            // Arrange
            var keyPair = KeyGenerator.GenerateKeys();
            _wallet.AddKeyPair(keyPair);
            var transactionData = "Sample transaction data";

            // Act
            var signature = _wallet.SignTransaction(transactionData);

            // Assert
            signature.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Should_Throw_When_Signing_Without_Active_Key()
        {
            // Arrange
            var transactionData = "Sample transaction data";

            // Act
            var act = () => _wallet.SignTransaction(transactionData);

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("No active key pair available to sign transaction.");
        }

        [Fact]
        public async Task Should_Handle_Multiple_Concurrent_Transaction_Signing()
        {
            // Arrange
            var keyPair1 = KeyGenerator.GenerateKeys();
            var keyPair2 = KeyGenerator.GenerateKeys();
            _wallet.AddKeyPair(keyPair1);
            _wallet.AddKeyPair(keyPair2);

            var transactionData = "Sample transaction data";

            var barrier = new Barrier(50);

            // Act: Prepare tasks that wait on the barrier and start them all together
            var tasks = new List<Task<string>>();
            for (int i = 0; i < 50; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    return _wallet.SignTransaction(transactionData);
                }));
            }

            var signatures = await Task.WhenAll(tasks);

            // Assert: All signatures should be non-null and valid
            signatures.Should().AllSatisfy(signature => signature.Should().NotBeNullOrEmpty());
        }


        [Fact]
        public async Task Should_Handle_Concurrent_Transactions_With_KeyPair_Switching()
        {
            // Arrange
            var keyPair1 = KeyGenerator.GenerateKeys();
            var keyPair2 = KeyGenerator.GenerateKeys();
            _wallet.AddKeyPair(keyPair1);
            _wallet.AddKeyPair(keyPair2);

            var transactionData1 = "Transaction from KeyPair1";
            var transactionData2 = "Transaction from KeyPair2";

            var barrier = new Barrier(100);

            // Act: Prepare tasks that wait on the barrier and start them all together
            var tasks = new List<Task<string>>();

            for (int i = 0; i < 50; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    barrier.SignalAndWait(); 
                    _wallet.SetActiveKeyPair(0);
                    return _wallet.SignTransaction(transactionData1);
                }));

                tasks.Add(Task.Run(() =>
                {
                    barrier.SignalAndWait(); 
                    _wallet.SetActiveKeyPair(1); 
                    return _wallet.SignTransaction(transactionData2);
                }));
            }

            var signatures = await Task.WhenAll(tasks);

            // Assert: Ensure signatures are not null and distinct between key pairs
            signatures.Should().AllSatisfy(signature => signature.Should().NotBeNullOrEmpty());
            signatures.Distinct().Count().Should().BeGreaterThan(1);
        }
    }
}