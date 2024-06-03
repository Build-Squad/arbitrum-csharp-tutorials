using System;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Hex.HexTypes;
using Nethereum.Contracts;
using static Arbitrum.DataEntities.NetworkUtils;
using Nethereum.Contracts.CQS;
using Nethereum.Contracts.ContractHandlers;

public static class TutorialsUtils
{
    public static async Task<BigInteger> CheckConfirmation(string txHash, Web3 l2Web3)
    {
        // Add the default local network configuration to the SDK
        // (Assuming some local network configuration function)
        AddDefaultLocalNetwork();

        // Call the related block hash
        string blockHash;
        try
        {
            var receipt = await l2Web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);
            blockHash = receipt.BlockHash;
        }
        catch (Exception e)
        {
            throw new Exception("Check blockNumber fail, reason: " + e.Message);
        }

        var nodeInterfaceAddress = NODE_INTERFACE_ADDRESS; // Define your constant
        var nodeInterface = new Contract(l2Web3, NodeInterfaceABI, nodeInterfaceAddress);

        BigInteger result;
        try
        {
            var getL1ConfirmationsFunction = nodeInterface.GetFunction("getL1Confirmations");
            result = await getL1ConfirmationsFunction.CallAsync<BigInteger>(blockHash);
        }
        catch (Exception e)
        {
            throw new Exception("Check fail, reason: " + e.Message);
        }

        return result;
    }

    public static async Task<string> FindSubmissionTx(string txHash, Web3 l1Web3, Web3 l2Web3)
    {
        // Add the default local network configuration to the SDK
        // (Assuming some local network configuration function)
        AddDefaultLocalNetwork();

        // Get the related block number
        BigInteger blockNumber;
        try
        {
            var receipt = await l2Web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);
            blockNumber = (BigInteger)receipt.BlockNumber.Value;
        }
        catch (Exception e)
        {
            throw new Exception("Check blockNumber fail, reason: " + e.Message);
        }

        var l2Network = await GetL2Network(l2Web3);
        var nodeInterfaceAddress = NODE_INTERFACE_ADDRESS; // Define your constant
        var nodeInterface = new Contract(l2Web3, NodeInterfaceABI, nodeInterfaceAddress);
        var sequencerInboxAddress = l2Network.EthBridge.SequencerInbox;
        var sequencerInbox = new Contract(l1Web3, SequencerInboxABI, sequencerInboxAddress);

        // Call the nodeInterface precompile to get the batch number first
        BigInteger batchNumber;
        try
        {
            var findBatchContainingBlockFunction = nodeInterface.GetFunction("findBatchContainingBlock");
            batchNumber = await findBatchContainingBlockFunction.CallAsync<BigInteger>(blockNumber);
        }
        catch (Exception e)
        {
            throw new Exception("Check l2 block fail, reason: " + e.Message);
        }

        // Query the SequencerBatchDelivered event
        var sequencerBatchDeliveredEvent = sequencerInbox.GetEvent("SequencerBatchDelivered");
        var filter = sequencerBatchDeliveredEvent.CreateFilterInput(batchNumber);
        var logs = await sequencerInbox.Web3.Eth.Filters.GetLogs.SendRequestAsync(filter);

        if (!logs.Any())
        {
            return string.Empty;
        }
        else
        {
            return logs.First().TransactionHash;
        }
    }
}
