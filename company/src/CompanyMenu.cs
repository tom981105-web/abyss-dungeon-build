using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace AgriculturalCompany;

internal sealed class CompanyMenu : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly List<(string Name, Rectangle Bounds)> Tabs = new();
    private int SelectedTab;

    private static readonly Color Green = new(48, 78, 58);
    private static readonly Color Green2 = new(78, 118, 84);
    private static readonly Color Accent = new(90, 128, 76);
    private static readonly Color Muted = new(105, 99, 82);

    internal CompanyMenu(ModEntry mod)
        : base(Game1.viewport.Width / 2 - 540, Game1.viewport.Height / 2 - 330, 1080, 660, true)
    {
        Mod = mod;
        Mod.Company.EnsureState();
        string[] names = { "대시보드", "생산", "창고", "계약", "거래처", "연구개발", "브랜드", "직원", "재무" };
        for (int i = 0; i < names.Length; i++)
            Tabs.Add((names[i], new Rectangle(xPositionOnScreen + 18, yPositionOnScreen + 104 + i * 50, 190, 40)));
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (upperRightCloseButton?.containsPoint(x, y) == true)
        {
            exitThisMenu();
            return;
        }

        for (int i = 0; i < Tabs.Count; i++)
        {
            if (!Tabs[i].Bounds.Contains(x, y))
                continue;
            SelectedTab = i;
            Game1.playSound("smallSelect");
            return;
        }
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.55f);
        drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

        Rectangle side = new(xPositionOnScreen + 8, yPositionOnScreen + 8, 215, height - 16);
        b.Draw(Game1.fadeToBlackRect, side, Green);
        DrawSidebar(b);

        if (SelectedTab == 0)
            DrawDashboard(b);
        else
            DrawComingSoon(b, Tabs[SelectedTab].Name);

        upperRightCloseButton?.draw(b);
        drawMouse(b);
    }

    private void DrawSidebar(SpriteBatch b)
    {
        b.DrawString(Game1.dialogueFont, "농업회사", new Vector2(xPositionOnScreen + 25, yPositionOnScreen + 23), Color.White);
        b.DrawString(Game1.smallFont, "STANDALONE 0.1", new Vector2(xPositionOnScreen + 27, yPositionOnScreen + 67), new Color(215, 228, 210));

        for (int i = 0; i < Tabs.Count; i++)
        {
            var tab = Tabs[i];
            if (i == SelectedTab)
                b.Draw(Game1.fadeToBlackRect, tab.Bounds, Green2);
            b.DrawString(Game1.smallFont, tab.Name, new Vector2(tab.Bounds.X + 16, tab.Bounds.Y + 8), Color.White);
        }

        b.DrawString(Game1.smallFont, "F7 회사 관리", new Vector2(xPositionOnScreen + 27, yPositionOnScreen + height - 48), new Color(215, 228, 210));
    }

    private void DrawDashboard(SpriteBatch b)
    {
        CompanySaveData c = Mod.State;
        int x = xPositionOnScreen + 250;
        int y = yPositionOnScreen + 28;
        int w = width - 285;

        b.DrawString(Game1.dialogueFont, c.CompanyName, new Vector2(x, y), Game1.textColor);
        b.DrawString(Game1.smallFont, $"Lv.{c.Level} · {CompanyCore.GetStageName(c.Level)}", new Vector2(x, y + 47), Accent);
        DrawXp(b, x, y + 77, w - 35);

        int cardY = y + 120;
        int gap = 12;
        int cardW = (w - gap * 3) / 4;
        DrawCard(b, x, cardY, cardW, "운영 자금", $"{Game1.player.Money:N0}G");
        DrawCard(b, x + (cardW + gap), cardY, cardW, "오늘 생산", $"{Mod.Company.GetTotal(c.TodayHarvest):N0}");
        DrawCard(b, x + (cardW + gap) * 2, cardY, cardW, "이번 계절", $"{Mod.Company.GetTotal(c.SeasonHarvest):N0}");
        DrawCard(b, x + (cardW + gap) * 3, cardY, cardW, "누적 생산", $"{Mod.Company.GetTotal(c.LifetimeHarvest):N0}");

        int sectionY = cardY + 125;
        b.DrawString(Game1.dialogueFont, "작물 생산 현황", new Vector2(x, sectionY), Game1.textColor);
        b.DrawString(Game1.smallFont, "기본 작물과 설치된 작물 모드의 생산량을 별도 데이터로 추적합니다.", new Vector2(x, sectionY + 39), Muted);

        int rowY = sectionY + 78;
        int rowW = (w - 36) / 4;
        DrawCropCard(b, x, rowY, rowW, "기본 작물", "Vanilla");
        DrawCropCard(b, x + rowW + 12, rowY, rowW, "수박 계열", "Watermelon");
        DrawCropCard(b, x + (rowW + 12) * 2, rowY, rowW, "참외 계열", "KoreanMelon");
        DrawCropCard(b, x + (rowW + 12) * 3, rowY, rowW, "배추", "NapaCabbage");

        int noteY = rowY + 175;
        drawTextureBox(b, x, noteY, w, 72, Color.White);
        b.DrawString(Game1.smallFont, "Agricultural Company 0.1 · Crop Genetics와 독립 실행", new Vector2(x + 18, noteY + 12), Accent);
        b.DrawString(Game1.smallFont, "다음: 0.2 창고 → 0.3 생산라인 → 0.4 납품 계약", new Vector2(x + 18, noteY + 40), Muted);
    }

    private void DrawXp(SpriteBatch b, int x, int y, int w)
    {
        CompanySaveData c = Mod.State;
        int start = CompanyCore.GetLevelStartXp(c.Level);
        int next = CompanyCore.GetNextLevelXp(c.Level);
        float p = c.Level >= 5 ? 1f : Math.Clamp((c.Experience - start) / (float)Math.Max(1, next - start), 0f, 1f);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(x, y, w, 16), new Color(215, 211, 195));
        b.Draw(Game1.fadeToBlackRect, new Rectangle(x, y, (int)(w * p), 16), Accent);
        string text = c.Level >= 5 ? $"회사 경험치 {c.Experience:N0} · 최고 단계" : $"회사 경험치 {c.Experience:N0} / {next:N0}";
        b.DrawString(Game1.smallFont, text, new Vector2(x + w - Game1.smallFont.MeasureString(text).X, y + 19), Muted);
    }

    private static void DrawCard(SpriteBatch b, int x, int y, int w, string label, string value)
    {
        drawTextureBox(b, x, y, w, 95, Color.White);
        b.DrawString(Game1.smallFont, label, new Vector2(x + 14, y + 17), Muted);
        b.DrawString(Game1.dialogueFont, value, new Vector2(x + 13, y + 43), Game1.textColor);
    }

    private void DrawCropCard(SpriteBatch b, int x, int y, int w, string title, string family)
    {
        CompanySaveData c = Mod.State;
        drawTextureBox(b, x, y, w, 150, Color.White);
        b.DrawString(Game1.smallFont, title, new Vector2(x + 14, y + 18), Game1.textColor);
        b.DrawString(Game1.smallFont, $"오늘 {Mod.Company.GetTotal(c.TodayHarvest, family):N0}", new Vector2(x + 14, y + 55), Muted);
        b.DrawString(Game1.smallFont, $"계절 {Mod.Company.GetTotal(c.SeasonHarvest, family):N0}", new Vector2(x + 14, y + 84), Muted);
        b.DrawString(Game1.smallFont, $"누적 {Mod.Company.GetTotal(c.LifetimeHarvest, family):N0}", new Vector2(x + 14, y + 113), Accent);
    }

    private void DrawComingSoon(SpriteBatch b, string tab)
    {
        int x = xPositionOnScreen + 285;
        int y = yPositionOnScreen + 155;
        int w = width - 350;
        drawTextureBox(b, x, y, w, 300, Color.White);
        b.DrawString(Game1.dialogueFont, tab, new Vector2(x, y - 65), Game1.textColor);
        string version = tab == "창고" ? "0.2" : tab == "생산" ? "0.3" : tab == "계약" ? "0.4" : "후속 업데이트";
        b.DrawString(Game1.dialogueFont, $"{tab} 시스템 준비 중", new Vector2(x + 40, y + 95), Accent);
        b.DrawString(Game1.smallFont, $"{version}에서 실제 기능이 연결됩니다.", new Vector2(x + 42, y + 155), Muted);
    }
}
