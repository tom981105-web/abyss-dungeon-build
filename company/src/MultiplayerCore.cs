using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace AgriculturalCompany;

internal sealed class MultiplayerCore
{
    private readonly ModEntry Mod;

    private const string MessageSyncRequest = "sync-request";
    private const string MessageStateSnapshot = "state-snapshot";
    private const string MessageHarvestReport = "harvest-report";
    private const string MessageWarehouseRequest = "warehouse-request";
    private const string MessageProductionRequest = "production-request";
    private const string MessageOperationResult = "operation-result";

    internal bool IsSynchronized { get; private set; }

    internal MultiplayerCore(ModEntry mod)
    {
        Mod = mod;
    }

    internal void Initialize()
    {
        Mod.Helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        Mod.Helper.Events.Multiplayer.PeerConnected += OnPeerConnected;
    }

    internal void OnSaveLoaded()
    {
        IsSynchronized = Context.IsMainPlayer;
        if (!Context.IsMainPlayer)
            RequestSync();
    }

    internal void RequestSync()
    {
        if (!Context.IsMultiplayer || Context.IsMainPlayer)
            return;

        Mod.Helper.Multiplayer.SendMessage(new SyncRequestMessage(), MessageSyncRequest, modIDs: new[] { Mod.ModManifest.UniqueID });
    }

    internal void BroadcastState()
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || !Context.IsMultiplayer)
            return;

        Mod.State.NetworkRevision++;
        Mod.Helper.Multiplayer.SendMessage(Mod.State, MessageStateSnapshot, modIDs: new[] { Mod.ModManifest.UniqueID });
    }

    internal void SendStateTo(long playerId)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        Mod.Helper.Multiplayer.SendMessage(Mod.State, MessageStateSnapshot, modIDs: new[] { Mod.ModManifest.UniqueID }, playerIDs: new[] { playerId });
    }

    internal void BroadcastNotice(string message)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || !Context.IsMultiplayer || string.IsNullOrWhiteSpace(message))
            return;

        Mod.Helper.Multiplayer.SendMessage(
            new OperationResultMessage { Success = true, Message = message },
            MessageOperationResult,
            modIDs: new[] { Mod.ModManifest.UniqueID });
    }

    internal void ReportHarvest(string itemId, int amount)
    {
        if (amount <= 0)
            return;

        if (Context.IsMainPlayer)
        {
            Mod.Company.ApplyHarvestReport(itemId, amount);
            BroadcastState();
            return;
        }

        Mod.Helper.Multiplayer.SendMessage(
            new HarvestReportMessage { ItemId = itemId, Amount = amount },
            MessageHarvestReport,
            modIDs: new[] { Mod.ModManifest.UniqueID });
    }

    internal void RequestWarehouse(string itemId, int amount, bool deposit, bool all)
    {
        if (Context.IsMainPlayer)
            return;

        Mod.Helper.Multiplayer.SendMessage(
            new WarehouseRequestMessage { ItemId = itemId, Amount = amount, Deposit = deposit, All = all },
            MessageWarehouseRequest,
            modIDs: new[] { Mod.ModManifest.UniqueID });
    }

    internal void RequestProduction(string recipeKey, int batches)
    {
        if (Context.IsMainPlayer)
            return;

        Mod.Helper.Multiplayer.SendMessage(
            new ProductionRequestMessage { RecipeKey = recipeKey, Batches = Math.Max(1, batches) },
            MessageProductionRequest,
            modIDs: new[] { Mod.ModManifest.UniqueID });
    }

    private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady)
            return;

        SendStateTo(e.Peer.PlayerID);
    }

    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (!string.Equals(e.FromModID, Mod.ModManifest.UniqueID, StringComparison.OrdinalIgnoreCase))
            return;

        if (e.Type == MessageSyncRequest)
        {
            if (Context.IsMainPlayer)
                SendStateTo(e.FromPlayerID);
            return;
        }

        if (e.Type == MessageStateSnapshot)
        {
            if (Context.IsMainPlayer)
                return;

            if (Game1.MasterPlayer is not null && e.FromPlayerID != Game1.MasterPlayer.UniqueMultiplayerID)
                return;

            CompanySaveData incoming = e.ReadAs<CompanySaveData>();
            if (incoming.NetworkRevision < Mod.State.NetworkRevision)
                return;

            Mod.ApplyNetworkState(incoming);
            IsSynchronized = true;
            return;
        }

        if (e.Type == MessageHarvestReport)
        {
            if (!Context.IsMainPlayer)
                return;

            HarvestReportMessage request = e.ReadAs<HarvestReportMessage>();
            Mod.Company.ApplyHarvestReport(request.ItemId, Math.Clamp(request.Amount, 0, 999));
            BroadcastState();
            return;
        }

        if (e.Type == MessageWarehouseRequest)
        {
            if (!Context.IsMainPlayer)
                return;

            HandleWarehouseRequest(e.FromPlayerID, e.ReadAs<WarehouseRequestMessage>());
            return;
        }

        if (e.Type == MessageProductionRequest)
        {
            if (!Context.IsMainPlayer)
                return;

            ProductionRequestMessage request = e.ReadAs<ProductionRequestMessage>();
            bool ok = Mod.Production.TryStartAuthoritative(request.RecipeKey, Math.Clamp(request.Batches, 1, 999), out string message);
            SendOperationResult(e.FromPlayerID, ok, message);
            if (ok)
                BroadcastState();
            return;
        }

        if (e.Type == MessageOperationResult && !Context.IsMainPlayer)
        {
            if (Game1.MasterPlayer is not null && e.FromPlayerID != Game1.MasterPlayer.UniqueMultiplayerID)
                return;

            OperationResultMessage result = e.ReadAs<OperationResultMessage>();
            Game1.addHUDMessage(result.Success
                ? new HUDMessage(result.Message)
                : new HUDMessage(result.Message, HUDMessage.error_type));
        }
    }

    private void HandleWarehouseRequest(long playerId, WarehouseRequestMessage request)
    {
        Farmer? player = Game1.getFarmer(playerId);
        if (player is null)
        {
            SendOperationResult(playerId, false, "플레이어 정보를 찾지 못했습니다.");
            return;
        }

        int requested = request.All ? int.MaxValue : Math.Clamp(request.Amount, 1, 9999);
        int moved = request.Deposit
            ? Mod.Company.DepositFromFarmer(player, request.ItemId, requested)
            : Mod.Company.WithdrawToFarmer(player, request.ItemId, request.All ? Mod.Company.GetWarehouseQuantity(request.ItemId) : requested);

        bool ok = moved > 0;
        string cropName = Mod.Company.FindCrop(request.ItemId)?.DisplayName ?? "품목";
        string verb = request.Deposit ? "입고" : "출고";
        string message = ok ? $"{cropName} {moved:N0}개 {verb} 완료." : $"{cropName} {verb} 요청을 처리하지 못했습니다.";
        SendOperationResult(playerId, ok, message);
        if (ok)
            BroadcastState();
    }

    private void SendOperationResult(long playerId, bool success, string message)
    {
        Mod.Helper.Multiplayer.SendMessage(
            new OperationResultMessage { Success = success, Message = message },
            MessageOperationResult,
            modIDs: new[] { Mod.ModManifest.UniqueID },
            playerIDs: new[] { playerId });
    }
}

public sealed class SyncRequestMessage { }

public sealed class HarvestReportMessage
{
    public string ItemId { get; set; } = "";
    public int Amount { get; set; }
}

public sealed class WarehouseRequestMessage
{
    public string ItemId { get; set; } = "";
    public int Amount { get; set; } = 1;
    public bool Deposit { get; set; }
    public bool All { get; set; }
}

public sealed class ProductionRequestMessage
{
    public string RecipeKey { get; set; } = "";
    public int Batches { get; set; } = 1;
}

public sealed class OperationResultMessage
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}
