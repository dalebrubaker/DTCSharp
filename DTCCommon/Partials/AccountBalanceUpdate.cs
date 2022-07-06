using Google.Protobuf;

// ReSharper disable once CheckNamespace
// ReSharper disable once IdentifierTypo
namespace DTCPB;

public partial class AccountBalanceUpdate : ICustomDiagnosticMessage
{
    public bool IsNoAccountBalances => NoAccountBalances != 0;

    public string ToDiagnosticString()
    {
        if (IsNoAccountBalances)
        {
            return $"No Account Balances {TradeAccount} RequestID={RequestID}";
        }
        var result =
            $"'{TradeAccount} Cash={CashBalance} Available={BalanceAvailableForNewPositions} MarginRequirement={MarginRequirement} Currency={AccountCurrency} Securities={SecuritiesValue} RequestID={RequestID}";
        return result;
    }
}