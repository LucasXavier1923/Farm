$ErrorActionPreference='Stop'
$project='D:\Dev\Unity\Farm\Farm';$cli='C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
function Replace-Once([string]$c,[string]$o,[string]$n,[string]$l){if(-not $c.Contains($o)){throw "Trecho nao encontrado: $l"};$c.Replace($o,$n)}
function Submit([string]$p,[string]$c,[string]$id){$payload=@{filePath=$p;content=$c;requestId=$id}|ConvertTo-Json -Compress;$r=$payload|& $cli run-tool script-update-or-create $project --input-file -;if($LASTEXITCODE-ne 0){throw "Falha: $p"};$r|Select-Object -Last 12;Start-Sleep -Seconds 2}

$plotPath=Join-Path $project 'Assets\_Project\Scripts\Farming\FarmTestPlot.cs';$plot=[IO.File]::ReadAllText($plotPath)
if(-not $plot.Contains('RequestOrderBoard')){
 $plot=Replace-Once $plot @'
        private const float SleepInteractionDistance = 1f;
'@ @'
        private const float SleepInteractionDistance = 1f;
        private const float OrderBoardInteractionDistance = 1f;
'@ 'board distance'
 $plot=Replace-Once $plot @'
        private readonly List<CropDefinition> shopCrops = new();
'@ @'
        private readonly List<CropDefinition> shopCrops = new();
        private readonly List<FarmDailyOrder> dailyOrders = new();
'@ 'order cache list'
 $plot=Replace-Once $plot @'
        private FarmSleepStation sleepStation;
'@ @'
        private FarmSleepStation sleepStation;
        private FarmOrderBoardStation orderBoardStation;
'@ 'board station field'
 $plot=Replace-Once $plot @'
        private FarmSleepStation hoveredSleep;
'@ @'
        private FarmSleepStation hoveredSleep;
        private FarmOrderBoardStation hoveredOrderBoard;
'@ 'board hover field'
 $plot=Replace-Once $plot @'
        private int shopCropIndex;
'@ @'
        private int shopCropIndex;
        private int cachedOrdersDay = -1;
        private int cachedOrdersSeed;
'@ 'order cache keys'
 $plot=Replace-Once $plot @'
        public bool SleepInRange => SleepVisible;
'@ @'
        public bool SleepInRange => SleepVisible;
        public bool OrderBoardVisible => orderBoardStation != null && IsOrderBoardInRange();
        public IReadOnlyList<FarmDailyOrder> DailyOrders
        {
            get
            {
                RefreshDailyOrdersCache();
                return dailyOrders;
            }
        }
'@ 'order properties'
 $plot=Replace-Once $plot @'
                    if (SleepVisible) RequestSleep();
                    else if (StorageVisible) RequestStorage();
'@ @'
                    if (SleepVisible) RequestSleep();
                    else if (OrderBoardVisible) RequestOrderBoard();
                    else if (StorageVisible) RequestStorage();
'@ 'E board priority'
 $plot=Replace-Once $plot @'
        public void RequestSleep()
'@ @'
        public void RequestOrderBoard()
        {
            if (!IsOrderBoardInRange())
            {
                feedback = "Chegue mais perto do quadro de pedidos.";
                return;
            }
            RefreshDailyOrdersCache();
            feedback = "Pedidos do dia abertos.";
            hud?.SetDailyOrdersOpen(true);
        }

        public void TryCompleteDailyOrder(int index)
        {
            if (!IsOrderBoardInRange())
            {
                feedback = "Chegue mais perto do quadro de pedidos.";
                return;
            }
            RefreshDailyOrdersCache();
            if (index < 0 || index >= dailyOrders.Count)
            {
                feedback = "Pedido inv\u00E1lido.";
                return;
            }
            if (gameState.TryCompleteDailyOrder(dailyOrders[index], index, out var earned, out var bonus, out var error))
            {
                feedback = bonus > 0
                    ? $"Pedido entregue: +${earned} (inclui b\u00F4nus de quadro completo de ${bonus})."
                    : $"Pedido entregue: +${earned}.";
                SaveGame(false);
            }
            else feedback = error;
        }

        private void RefreshDailyOrdersCache()
        {
            if (gameState == null) return;
            if (cachedOrdersDay == gameState.DayNumber && cachedOrdersSeed == gameState.WorldSeed && dailyOrders.Count > 0) return;
            cachedOrdersDay = gameState.DayNumber;
            cachedOrdersSeed = gameState.WorldSeed;
            dailyOrders.Clear();
            dailyOrders.AddRange(FarmDailyOrderGenerator.Generate(gameState.WorldSeed, gameState.DayNumber));
        }

        public void RequestSleep()
'@ 'order actions'
 $plot=Replace-Once $plot @'
            CreateSleepStation(center, forward, offset);
'@ @'
            CreateSleepStation(center, forward, offset);
            CreateOrderBoardStation(center, forward, offset);
'@ 'create board call'
 $plot=Replace-Once $plot @'
        private static Vector3 PlaceOnGround(Vector3 position)
'@ @'
        private void CreateOrderBoardStation(Vector3 plotCenter, Vector3 forward, float gridOffset)
        {
            var boardPrefab = Resources.Load<GameObject>("FarmProps/OrderBoard");
            GameObject stationObject;
            if (boardPrefab != null) stationObject = Instantiate(boardPrefab);
            else
            {
                stationObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stationObject.transform.localScale = new Vector3(1.8f, 1.4f, 0.25f);
                Debug.LogWarning("OrderBoard n\u00E3o encontrado; usando quadro tempor\u00E1rio.");
            }
            stationObject.name = "Farm_Daily_Order_Board";
            stationObject.transform.SetParent(transform, true);
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            stationObject.transform.position = PlaceOnGround(plotCenter + (forward * (gridOffset + 4.3f)) - (right * 1.8f));
            stationObject.transform.rotation = Quaternion.LookRotation(-forward, Vector3.up);
            stationObject.transform.localScale *= 0.9f;
            if (stationObject.GetComponentInChildren<Collider>() == null) stationObject.AddComponent<BoxCollider>();
            orderBoardStation = stationObject.AddComponent<FarmOrderBoardStation>();
            orderBoardStation.Initialize();

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Order_Board_Cyan_Marker";
            marker.transform.SetParent(stationObject.transform, false);
            marker.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            marker.transform.localScale = new Vector3(0.20f, 0.06f, 0.20f);
            var markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null) Destroy(markerCollider);
            marker.GetComponent<Renderer>().material.color = new Color(0.22f, 0.88f, 0.82f);
        }

        private static Vector3 PlaceOnGround(Vector3 position)
'@ 'board creation'
 $plot=Replace-Once $plot @'
            SetHoveredSleep(null);
'@ @'
            SetHoveredSleep(null);
            SetHoveredOrderBoard(null);
'@ 'clear board hover'
 $plot=Replace-Once $plot @'
                var sleep = hit.collider.GetComponentInParent<FarmSleepStation>();
                if (sleep != null)
                {
                    SetHoveredSleep(sleep);
                    return;
                }
'@ @'
                var sleep = hit.collider.GetComponentInParent<FarmSleepStation>();
                if (sleep != null)
                {
                    SetHoveredSleep(sleep);
                    return;
                }

                var orderBoard = hit.collider.GetComponentInParent<FarmOrderBoardStation>();
                if (orderBoard != null)
                {
                    SetHoveredOrderBoard(orderBoard);
                    return;
                }
'@ 'raycast board'
 $plot=Replace-Once $plot @'
        private bool IsInRange(Vector3 targetPosition) =>
'@ @'
        private void SetHoveredOrderBoard(FarmOrderBoardStation station)
        {
            if (hoveredOrderBoard == station) return;
            if (hoveredOrderBoard != null) hoveredOrderBoard.SetHovered(false);
            hoveredOrderBoard = station;
            if (hoveredOrderBoard != null) hoveredOrderBoard.SetHovered(true);
        }

        private bool IsInRange(Vector3 targetPosition) =>
'@ 'board hover setter'
 $plot=Replace-Once $plot @'
        private void TryPrimaryInteraction()
'@ @'
        private bool IsOrderBoardInRange() =>
            player != null && orderBoardStation != null && Vector3.Distance(player.position, orderBoardStation.transform.position) <= OrderBoardInteractionDistance;

        private void TryPrimaryInteraction()
'@ 'board range'
 $plot=Replace-Once $plot @'
            else if (hoveredSleep != null) RequestSleep();
'@ @'
            else if (hoveredSleep != null) RequestSleep();
            else if (hoveredOrderBoard != null) RequestOrderBoard();
'@ 'mouse board'
 $plot=Replace-Once $plot @'
            if (SleepVisible) return "Cama ao alcance: pressione E para descansar.";
'@ @'
            if (hoveredOrderBoard != null)
            {
                var range = IsOrderBoardInRange() ? "" : " (fora de alcance)";
                return $"Quadro de pedidos: clique ou E para ver entregas{range}";
            }
            if (OrderBoardVisible) return "Quadro ao alcance: pressione E ou P para abrir os pedidos.";
            if (SleepVisible) return "Cama ao alcance: pressione E para descansar.";
'@ 'board prompt'
 $plot=Replace-Once $plot @'
    public sealed class FarmSleepStation : MonoBehaviour
'@ @'
    public sealed class FarmOrderBoardStation : MonoBehaviour
    {
        private Vector3 normalScale;
        public void Initialize() => normalScale = transform.localScale;
        public void SetHovered(bool hovered) => transform.localScale = normalScale * (hovered ? 1.05f : 1f);
    }

    public sealed class FarmSleepStation : MonoBehaviour
'@ 'board component'
 Submit 'Assets/_Project/Scripts/Farming/FarmTestPlot.cs' $plot 'daily-orders-world-v11'
}

$hudPath=Join-Path $project 'Assets\_Project\Scripts\Farming\FarmHudController.cs';$hud=[IO.File]::ReadAllText($hudPath)
if(-not $hud.Contains('CreateDailyOrdersWindow')){
 $hud=Replace-Once $hud @'
        private Coroutine dayTransitionRoutine;
'@ @'
        private Coroutine dayTransitionRoutine;
        private GameObject dailyOrdersWindow;
        private CanvasGroup dailyOrdersGroup;
        private bool dailyOrdersOpen;
        private Text dailyOrdersSummaryText;
        private readonly Text[] dailyOrderTexts = new Text[FarmDailyOrderGenerator.OrderCount];
        private readonly Button[] dailyOrderButtons = new Button[FarmDailyOrderGenerator.OrderCount];
        private readonly Text[] dailyOrderButtonTexts = new Text[FarmDailyOrderGenerator.OrderCount];
'@ 'order UI fields'
 $hud=Replace-Once $hud @'
        public bool IsSleepConfirmationOpen => sleepConfirmationOpen;
'@ @'
        public bool IsSleepConfirmationOpen => sleepConfirmationOpen;
        public bool IsDailyOrdersOpen => dailyOrdersOpen;
'@ 'order UI property'
 $hud=Replace-Once $hud @'
            RefreshJournal(state);
'@ @'
            RefreshJournal(state);
            RefreshDailyOrders(state);
'@ 'refresh orders'
 $hud=$hud.Replace('if (value && sleepConfirmationOpen) SetSleepConfirmationOpen(false);',"if (value && sleepConfirmationOpen) SetSleepConfirmationOpen(false);`n            if (value && dailyOrdersOpen) SetDailyOrdersOpen(false);")
 $hud=Replace-Once $hud @'
            if (value && journalOpen) SetJournalOpen(false);
            sleepConfirmationOpen = value;
'@ @'
            if (value && journalOpen) SetJournalOpen(false);
            if (value && dailyOrdersOpen) SetDailyOrdersOpen(false);
            sleepConfirmationOpen = value;
'@ 'sleep closes orders'
 $hud=Replace-Once $hud @'
        public void ConfirmSleep()
'@ @'
        public void SetDailyOrdersOpen(bool value)
        {
            if (value && inventoryOpen) SetInventoryOpen(false);
            if (value && storageOpen) SetStorageOpen(false);
            if (value && journalOpen) SetJournalOpen(false);
            if (value && sleepConfirmationOpen) SetSleepConfirmationOpen(false);
            dailyOrdersOpen = value;
            SetCanvasGroup(dailyOrdersGroup, value);
            if (value) dailyOrdersWindow.transform.SetAsLastSibling();
            UpdateModalState();
        }

        public void CompleteDailyOrder(int index) => plot?.TryCompleteDailyOrder(index);

        public void ConfirmSleep()
'@ 'order UI API'
 $hud=Replace-Once $hud @'
        private void UpdateModalState() => IsModalOpen = inventoryOpen || storageOpen || journalOpen || sleepConfirmationOpen;
'@ @'
        private void UpdateModalState() => IsModalOpen = inventoryOpen || storageOpen || journalOpen || sleepConfirmationOpen || dailyOrdersOpen;
'@ 'orders modal'
 $hud=Replace-Once $hud @'
            if (keyboard.jKey.wasPressedThisFrame)
'@ @'
            if (keyboard.pKey.wasPressedThisFrame)
            {
                if (dailyOrdersOpen) SetDailyOrdersOpen(false);
                else plot?.RequestOrderBoard();
            }
            else if (keyboard.jKey.wasPressedThisFrame)
'@ 'P shortcut'
 $hud=Replace-Once $hud @'
                if (sleepConfirmationOpen) SetSleepConfirmationOpen(false);
'@ @'
                if (sleepConfirmationOpen) SetSleepConfirmationOpen(false);
                if (dailyOrdersOpen) SetDailyOrdersOpen(false);
'@ 'escape closes orders'
 $hud=Replace-Once $hud @'
        private void RefreshInventory(FarmGameState state)
'@ @'
        private void RefreshDailyOrders(FarmGameState state)
        {
            if (dailyOrdersSummaryText == null || plot == null) return;
            var orders = plot.DailyOrders;
            var completed = 0;
            for (var index = 0; index < dailyOrderTexts.Length; index++)
            {
                if (index >= orders.Count)
                {
                    dailyOrderTexts[index].text = "Pedido indispon\u00EDvel";
                    dailyOrderButtons[index].interactable = false;
                    dailyOrderButtonTexts[index].text = "INDISPON\u00CDVEL";
                    continue;
                }
                var order = orders[index];
                var crop = order.Crop;
                var have = crop != null ? state.GetQuantity(crop.HarvestItem.Id) : 0;
                var done = state.DailyOrders.IsCompleted(index);
                if (done) completed++;
                dailyOrderTexts[index].text = $"{order.DisplayText}\nVoc\u00EA possui: {have}/{order.Quantity}\nRecompensa: ${order.Reward}";
                dailyOrderButtons[index].interactable = !done && have >= order.Quantity && plot.OrderBoardVisible;
                dailyOrderButtonTexts[index].text = done ? "ENTREGUE" : have >= order.Quantity ? "ENTREGAR" : $"FALTAM {order.Quantity - have}";
            }
            dailyOrdersSummaryText.text = $"DIA {state.DayNumber}  \u2022  {completed}/{orders.Count} entregues  \u2022  Complete os 3: +${FarmDailyOrderGenerator.BoardCompletionBonus}";
        }

        private void RefreshInventory(FarmGameState state)
'@ 'refresh order cards'
 $hud=Replace-Once $hud 'dayTransitionGroup == null || dayTransitionText == null) return false;' 'dayTransitionGroup == null || dayTransitionText == null || dailyOrdersWindow == null || dailyOrdersGroup == null || dailyOrdersSummaryText == null) return false;' 'interface order UI'
 $hud=Replace-Once $hud @'
            CreateSleepConfirmation(root.transform);
            CreateDayTransition(root.transform);
'@ @'
            CreateSleepConfirmation(root.transform);
            CreateDailyOrdersWindow(root.transform);
            CreateDayTransition(root.transform);
'@ 'create order UI call'
 $hud=Replace-Once $hud @'
        private void CreateSleepConfirmation(Transform root)
'@ @'
        private void CreateDailyOrdersWindow(Transform root)
        {
            dailyOrdersWindow = CreatePanel("DailyOrdersWindow", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.01f, 0.02f, 0.018f, 0.76f));
            var backdrop = dailyOrdersWindow.GetComponent<RectTransform>();
            backdrop.offsetMin = Vector2.zero;
            backdrop.offsetMax = Vector2.zero;
            dailyOrdersGroup = dailyOrdersWindow.AddComponent<CanvasGroup>();
            var panel = CreatePanel("DailyOrdersPanel", dailyOrdersWindow.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040f, 620f), new Vector2(0.5f, 0.5f), PanelColor);
            CreateText("Title", panel.transform, "PEDIDOS DO DIA", 30, FontStyle.Bold, new Color(0.30f, 0.92f, 0.82f), new Vector2(30f, -20f), new Vector2(700f, 42f));
            dailyOrdersSummaryText = CreateText("Summary", panel.transform, "", 17, FontStyle.Bold, Color.white, new Vector2(30f, -66f), new Vector2(820f, 30f));
            var close = CreateButton("CloseOrders", panel.transform, "FECHAR  [P]", new Vector2(850f, -22f), new Vector2(160f, 44f));
            close.onClick.AddListener(() => SetDailyOrdersOpen(false));
            for (var index = 0; index < dailyOrderTexts.Length; index++)
            {
                var card = CreatePanel($"DailyOrder_{index + 1}", panel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(35f, -125f - (index * 145f)), new Vector2(970f, 125f), new Vector2(0f, 1f), SlotColor);
                dailyOrderTexts[index] = CreateText("OrderText", card.transform, "", 19, FontStyle.Bold, Color.white, new Vector2(22f, -14f), new Vector2(690f, 96f));
                var captured = index;
                dailyOrderButtons[index] = CreateButton("Deliver", card.transform, "ENTREGAR", new Vector2(735f, -35f), new Vector2(205f, 56f));
                dailyOrderButtonTexts[index] = dailyOrderButtons[index].GetComponentInChildren<Text>();
                dailyOrderButtons[index].onClick.AddListener(() => CompleteDailyOrder(captured));
            }
            CreateText("Hint", panel.transform, "Pedidos mudam a cada manh\u00E3. Entregas contam como vendas no di\u00E1rio.", 15, FontStyle.Normal, new Color(0.76f, 0.84f, 0.78f), new Vector2(35f, -570f), new Vector2(850f, 26f));
            SetCanvasGroup(dailyOrdersGroup, false);
        }

        private void CreateSleepConfirmation(Transform root)
'@ 'order UI builder'
 Submit 'Assets/_Project/Scripts/Farming/FarmHudController.cs' $hud 'daily-orders-ui-v11'
}
Write-Output 'Daily orders world/UI v11 submitted.'
