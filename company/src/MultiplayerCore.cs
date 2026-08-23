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
    private const string MessageContractRequest = "contract-request";
    private const string MessageOperationResult = "operation-result";
    private const string MessageWarehouseLockRequest = "warehouse-lock-request";
    private const string MessageWarehouseLockRelease = "warehouse-lock-release";
    private const string MessageWarehouseLockHeartbeat = "warehouse-lock-heartbeat";
    private const string MessageWarehouseLockState = "warehouse-lock-state";

    private DateTime WarehouseLockHeartbeatUtc = DateTime.MinValue;

    internal bool IsSynchronized { get; private set; }
    internal long WarehouseLockOwnerId { get; private set; }
    internal string WarehouseLockOwnerName { get; private set; } = "";

    internal bool LocalHasWarehouseControl
        => !Context.IsMultiplayer || (Context.IsWorldReady && WarehouseLockOwnerId == Game1.player.UniqueMultiplayerID);

    internal bool WarehouseLockedByOther
        => Context.IsMultiplayer && WarehouseLockOwnerId != 0 && (!Context.IsWorldReady || WarehouseLockOwnerId != Game1.player.UniqueMultiplayerID);

    internal MultiplayerCore(ModEntry mod)
    {
        Mod = mod;
    }

    internal void Initialize()
    {
        Mod.Helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        Mod.Helper.Events.Multiplayer.PeerConnected += OnPeerConnected;
        Mod.Helper.Events.Multiplayer.PeerDisconnected += OnPeerDisconnected;
        Mod.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }

    internal void OnSaveLoaded()
    {
        IsSynchronized = Context.IsMainPlayer;
        ResetWarehouseLockLocal();
        if (!Context.IsMainPlayer)
            RequestSync();
    }

    internal void OnDayStarted()
    {
        ResetWarehouseLockLocal();
        if (Context.IsMainPlayer)
        {
            BroadcastState();
            BroadcastWarehouseLockState();
        }
        else
        {
            RequestSync();
        }
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
        Mod.Helper.Multiplayer.SendMessage(new OperationResultMessage { Success = true, Message = message }, MessageOperationResult, modIDs: new[] { Mod.ModManifest.UniqueID });
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

        Mod.Helper.Multiplayer.SendMessage(new HarvestReportMessage { ItemId = itemId, Amount = amount }, MessageHarvestReport, modIDs: new[] { Mod.ModManifest.UniqueID });
    }

    internal void RequestWarehouse(string itemId, int amount, bool deposit, bool all)
    {
        if (Context.IsMainPlayer)
            return;
        Mod.Helper.Multiplayer.SendMessage(new WarehouseRequestMessage { ItemId = itemId, Amount = amount, Deposit = deposit, All = all }, MessageWarehouseRequest, modIDs: new[] { Mod.ModManifest.UniqueID });
    }

    internal void RequestProduction(string recipeKey, int batches)
    {
        if (Context.IsMainPlayer)
            return;
        Mod.Helper.Multiplayer.SendMessage(new ProductionRequestMessage { RecipeKey = recipeKey, Batches = Math.Max(1, batches) }, MessageProductionRequest, modIDs: new[] { Mod.ModManifest.UniqueID });
    }

    internal void RequestContract(string contractId, string action)
    {
        if (Context.IsMainPlayer)
            return;
        Mod.Helper.Multiplayer.SendMessage(new ContractRequestMessage { ContractId = contractId, Action = action }, MessageContractRequest, modIDs: new[] { Mod.ModManifest.UniqueID });
    }

    internal void RequestWarehouseControl()
    {
        if (!Context.IsWorldReady)
            return;

        if (!Context.IsMultiplayer)
        {
            WarehouseLockOwnerId = Game1.player.UniqueMultiplayerID;
            WarehouseLockOwnerName = Game1.player.Name;
            return;
        }

        if (Context.IsMainPlayer)
        {
            TryAcquireWarehouseLock(Game1.player.UniqueMultiplayerID, Game1.player.Name, false);
            return;
        }

        Mod.Helper.Multiplayer.SendMessage(new WarehouseLockRequestMessage { PlayerName = Game1.player.Name }, MessageWarehouseLockRequest, modIDs: new[] { Mod.ModManifest.UniqueID });
    }

    internal void ReleaseWarehouseControl()
    {
        if (!Context.IsMultiplayer || !Context.IsWorldReady)
            return;

        long localId = Game1.player.UniqueMultiplayerID;
        if (Context.IsMainPlayer)
        {
            if (WarehouseLockOwnerId == localId)
                ClearWarehouseLockAndBroadcast();
            return;
        }

        Mod.Helper.Multiplayer.SendMessage(new WarehouseLockReleaseMessage(), MessageWarehouseLockRelease, modIDs: new[] { Mod.ModManifest.UniqueID });
    }

    internal string GetWarehouseControlStatus()
    {
        if (!Context.IsMultiplayer)
            return "창고 관리 가능";
        if (LocalHasWarehouseControl)
            return "내가 창고 관리 중";
        if (WarehouseLockOwnerId == 0)
            return "창고 사용권 확인 중";
        return $"{WarehouseLockOwnerName}님이 창고 관리 중 · 열람 전용";
    }

    private void TouchWarehouseControl()
    {
        if (!Context.IsMultiplayer || !Context.IsWorldReady || !LocalHasWarehouseControl)
            return;

        if (Context.IsMainPlayer)
        {
            WarehouseLockHeartbeatUtc = DateTime.UtcNow;
            return;
        }

        Mod.Helper.Multiplayer.SendMessage(new WarehouseLockHeartbeatMessage(), MessageWarehouseLockHeartbeat, modIDs: new[] { Mod.ModManifest.UniqueID });
    }

    private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady)
            return;
        SendStateTo(e.Peer.PlayerID);
        SendWarehouseLockStateTo(e.Peer.PlayerID);
    }

    private void OnPeerDisconnected(object? sender, PeerDisconnectedEventArgs e)
    {
        if (Context.IsMainPlayer && WarehouseLockOwnerId == e.Peer.PlayerID)
            ClearWarehouseLockAndBroadcast();
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMultiplayer || !e.IsMultipleOf(300))
            return;

        if (Game1.activeClickableMenu is CompanyMenu menu && menu.IsWarehouseTabOpen && LocalHasWarehouseControl)
            TouchWarehouseControl();

        if (!Context.IsMainPlayer || WarehouseLockOwnerId == 0 || WarehouseLockHeartbeatUtc == DateTime.MinValue)
            return;
        if ((DateTime.UtcNow - WarehouseLockHeartbeatUtc).TotalSeconds > 20)
            ClearWarehouseLockAndBroadcast();
    }

    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (!string.Equals(e.FromModID, Mod.ModManifest.UniqueID, StringComparison.OrdinalIgnoreCase))
            return;

        if (e.Type == MessageSyncRequest)
        {
            if (Context.IsMainPlayer)
            {
                SendStateTo(e.FromPlayerID);
                SendWarehouseLockStateTo(e.FromPlayerID);
            }
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

        if (e.Type == MessageWarehouseLockRequest)
        {
            if (!Context.IsMainPlayer)
                return;
            WarehouseLockRequestMessage request = e.ReadAs<WarehouseLockRequestMessage>();
            Farmer? farmer = Game1.getFarmer(e.FromPlayerID);
            TryAcquireWarehouseLock(e.FromPlayerID, farmer?.Name ?? request.PlayerName ?? "공동 경영자", true);
            return;
        }

        if (e.Type == MessageWarehouseLockRelease)
        {
            if (Context.IsMainPlayer && WarehouseLockOwnerId == e.FromPlayerID)
                ClearWarehouseLockAndBroadcast();
            return;
        }

        if (e.Type == MessageWarehouseLockHeartbeat)
        {
            if (Context.IsMainPlayer && WarehouseLockOwnerId == e.FromPlayerID)
                WarehouseLockHeartbeatUtc = DateTime.UtcNow;
            return;
        }

        if (e.Type == MessageWarehouseLockState)
        {
            if (Context.IsMainPlayer)
                return;
            if (Game1.MasterPlayer is not null && e.FromPlayerID != Game1.MasterPlayer.UniqueMultiplayerID)
                return;
            WarehouseLockStateMessage state = e.ReadAs<WarehouseLockStateMessage>();
            WarehouseLockOwnerId = state.OwnerId;
            WarehouseLockOwnerName = state.OwnerName ?? "";
            return;
        }

        if (e.Type == MessageWarehouseRequest)
        {
            if (Context.IsMainPlayer)
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

        if (e.Type == MessageContractRequest)
        {
            if (!Context.IsMainPlayer)
                return;
            ContractRequestMessage request = e.ReadAs<ContractRequestMessage>();
            bool ok;
            string message;
            if (string.Equals(request.Action, "accept", StringComparison.OrdinalIgnoreCase))
                ok = Mod.Contracts.TryAcceptAuthoritative(request.ContractId, out message);
            else if (string.Equals(request.Action, "deliver", StringComparison.OrdinalIgnoreCase))
                ok = Mod.Contracts.TryDeliverAuthoritative(request.ContractId, out message);
            else
            {
                ok = false;
                message = "알 수 없는 계약 작업입니다.";
            }
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
            Game1.addHUDMessage(result.Success ? new HUDMessage(result.Message) : new HUDMessage(result.Message, HUDMessage.error_type));
        }
    }

    private void TryAcquireWarehouseLock(long playerId, string playerName, bool sendResult)
    {
        if (!Context.IsMainPlayer)
            return;

        if (WarehouseLockOwnerId == 0 || WarehouseLockOwnerId == playerId)
        {
            WarehouseLockOwnerId = playerId;
            WarehouseLockOwnerName = string.IsNullOrWhiteSpace(playerName) ? "공동 경영자" : playerName;
            WarehouseLockHeartbeatUtc = DateTime.UtcNow;
            BroadcastWarehouseLockState();
            if (sendResult)
                SendOperationResult(playerId, true, "회사 창고 관리권을 받았습니다.");
            return;
        }

        if (sendResult)
            SendOperationResult(playerId, false, $"{WarehouseLockOwnerName}님이 창고를 관리 중입니다. 지금은 열람만 가능합니다.");
        SendWarehouseLockStateTo(playerId);
    }

    private void HandleWarehouseRequest(long playerId, WarehouseRequestMessage request)
    {
        if (WarehouseLockOwnerId != playerId)
        {
            SendOperationResult(playerId, false, WarehouseLockOwnerId == 0 ? "창고 관리권을 먼저 받아야 합니다." : $"{WarehouseLockOwnerName}님이 창고를 관리 중입니다.");
            SendWarehouseLockStateTo(playerId);
            return;
        }

        WarehouseLockHeartbeatUtc = DateTime.UtcNow;
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
        SendOperationResult(playerId, ok, ok ? $"{cropName} {moved:N0}개 {verb} 완료." : $"{cropName} {verb} 요청을 처리하지 못했습니다.");
        if (ok)
            BroadcastState();
    }

    private void ResetWarehouseLockLocal()
    {
        WarehouseLockOwnerId = 0;
        WarehouseLockOwnerName = "";
        WarehouseLockHeartbeatUtc = DateTime.MinValue;
    }

    private void ClearWarehouseLockAndBroadcast()
    {
        ResetWarehouseLockLocal();
        BroadcastWarehouseLockState();
    }

    private void BroadcastWarehouseLockState()
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady || !Context.IsMultiplayer)
            return;
        Mod.Helper.Multiplayer.SendMessage(new WarehouseLockStateMessage { OwnerId = WarehouseLockOwnerId, OwnerName = WarehouseLockOwnerName }, MessageWarehouseLockState, modIDs: new[] { Mod.ModManifest.UniqueID });
    }

    private void SendWarehouseLockStateTo(long playerId)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady)
            return;
        Mod.Helper.Multiplayer.SendMessage(new WarehouseLockStateMessage { OwnerId = WarehouseLockOwnerId, OwnerName = WarehouseLockOwnerName }, MessageWarehouseLockState, modIDs: new[] { Mod.ModManifest.UniqueID }, playerIDs: new[] { playerId });
    }

    private void SendOperationResult(long playerId, bool success, string message)
    {
        Mod.Helper.Multiplayer.SendMessage(new OperationResultMessage { Success = success, Message = message }, MessageOperationResult, modIDs: new[] { Mod.ModManifest.UniqueID }, playerIDs: new[] { playerId });
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

public sealed class ContractRequestMessage
{
    public string ContractId { get; set; } = "";
    public string Action { get; set; } = "";
}

public sealed class WarehouseLockRequestMessage
{
    public string PlayerName { get; set; } = "";
}

public sealed class WarehouseLockReleaseMessage { }
public sealed class WarehouseLockHeartbeatMessage { }

public sealed class WarehouseLockStateMessage
{
    public long OwnerId { get; set; }
    public string OwnerName { get; set; } = "";
}

public sealed class OperationResultMessage
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}
