using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace AgriculturalCompany;

internal sealed class ProductCatalog091Menu : Company091UiBase
{
    private int Page;
    private int Filter;
    private string SelectedKey;
    private string Message = "제품 카드를 선택하면 상세 생산 정보를 확인할 수 있습니다.";

    internal ProductCatalog091Menu(ModEntry mod, string selectedKey = "") : base(mod)
    {
        Mod.Production.EnsureState();
        SelectedKey = selectedKey;
    }

    private List<ProductionRecipeDefinition> Rows()
    {
        IEnumerable<ProductionRecipeDefinition> q = Mod.Production.GetCatalogRecipes(true);
        if (Filter == 1) q = q.Where(p => string.Equals(p.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase));
        else if (Filter == 2) q = q.Where(p => !string.Equals(p.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase));
        return q.ToList();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (Close().Contains(x, y) || Back().Contains(x, y)) { Game1.playSound("bigDeSelect"); Game1.activeClickableMenu = new Production091Menu(Mod); return; }
        for (int i = 0; i < 3; i++) if (FilterBtn(i).Contains(x, y)) { Filter = i; Page = 0; Game1.playSound("smallSelect"); return; }

        List<ProductionRecipeDefinition> rows = Rows(); int start = Page * 6;
        for (int i = 0; i < 6; i++)
        {
            int idx = start + i; if (idx >= rows.Count) break;
            if (RecipeCard(i).Contains(x, y)) { SelectedKey = rows[idx].Key; Game1.playSound("smallSelect"); return; }
        }

        ProductionRecipeDefinition? selected = Mod.Production.FindRecipe(SelectedKey);
        if (selected is not null && OneBatch().Contains(x, y))
        {
            if (!Mod.Production.IsRecipeUnlocked(selected, out string reason)) { Message = reason; Game1.playSound("cancel"); return; }
            bool ok = Mod.Production.TryStart(selected.Key, 1, out string m); Message = m; Game1.playSound(ok ? "Ship" : "cancel"); return;
        }
        if (selected is not null && MaxBatch().Contains(x, y))
        {
            if (!Mod.Production.IsRecipeUnlocked(selected, out string reason)) { Message = reason; Game1.playSound("cancel"); return; }
            int max = Math.Min(10, Mod.Production.GetMaxBatches(selected));
            if (max <= 0) { Message = $"{Mod.Production.GetIngredientDisplayName(selected)} 재고가 부족합니다."; Game1.playSound("cancel"); return; }
            bool ok = Mod.Production.TryStart(selected.Key, max, out string m); Message = m; Game1.playSound(ok ? "Ship" : "cancel"); return;
        }

        int maxPage = Math.Max(0, (rows.Count - 1) / 6);
        if (Prev().Contains(x, y) && Page > 0) { Page--; Game1.playSound("shwip"); }
        else if (Next().Contains(x, y) && Page < maxPage) { Page++; Game1.playSound("shwip"); }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        List<ProductionRecipeDefinition> rows = Rows(); int max = Math.Max(0, (rows.Count - 1) / 6);
        if (direction < 0 && Page < max) Page++; else if (direction > 0 && Page > 0) Page--;
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.70f);
        Frame(b); Header(b); Filters(b); Cards(b); Detail(b); Footer(b); drawMouse(b);
    }

    private void Header(SpriteBatch b)
    {
        Button(b, Back(), "생산 관리", false);
        Plaque(b, D(300, 8, 680, 58), "생산품 카탈로그", 1.10f);
        Button(b, Close(), "X", false, new Color(188,75,44));
    }

    private void Filters(SpriteBatch b)
    {
        string[] names={"전체 제품","중간재","완제품"};
        for(int i=0;i<3;i++) Button(b,FilterBtn(i),names[i],Filter==i,Filter==i?Green:null);
        Text(b,Game1.smallFont,"제품을 선택하면 재료 · 시간 · 생산량 · 해금 조건을 오른쪽에서 확인할 수 있습니다.",D(470,87,770,26),Muted,0.72f);
    }

    private void Cards(SpriteBatch b)
    {
        Panel(b,D(20,125,755,515)); Plaque(b,D(38,123,719,37),"제품 목록",0.80f);
        List<ProductionRecipeDefinition> rows=Rows(); int start=Page*6;
        for(int i=0;i<6;i++)
        {
            Rectangle r=RecipeCard(i); Card(b,r); int idx=start+i;
            if(idx>=rows.Count){Text(b,Game1.smallFont,"빈 슬롯",r,Muted,0.68f,true);continue;}
            ProductionRecipeDefinition recipe=rows[idx]; bool unlocked=Mod.Production.IsRecipeUnlocked(recipe,out string reason); bool selected=string.Equals(recipe.Key,SelectedKey,StringComparison.OrdinalIgnoreCase);
            if(selected){Fill(b,new Rectangle(r.X,r.Y,S(7),r.Height),Green);Fill(b,new Rectangle(r.X,r.Y,r.Width,S(5)),Gold);}
            DrawProduct(b,recipe,new Rectangle(r.X+S(12),r.Y+S(14),S(116),S(116)),unlocked?1f:0.34f);
            Text(b,Game1.dialogueFont,recipe.DisplayName,new Rectangle(r.X+S(142),r.Y+S(12),r.Width-S(155),S(37)),unlocked?Ink:Muted,0.69f);
            string kind=string.Equals(recipe.OutputKind,"Intermediate",StringComparison.OrdinalIgnoreCase)?"중간재":"완제품";
            Text(b,Game1.smallFont,$"{kind} · {LineName(recipe.LineType)} 라인",new Rectangle(r.X+S(143),r.Y+S(52),r.Width-S(156),S(24)),kind=="중간재"?Blue:Green,0.68f);
            string ingredient=Mod.Production.GetIngredientDisplayName(recipe); int have=Mod.Production.GetIngredientQuantity(recipe); int max=Mod.Production.GetMaxBatches(recipe);
            Text(b,Game1.smallFont,$"{ingredient} × {recipe.InputQuantity} → {recipe.OutputQuantity}{recipe.OutputUnit}",new Rectangle(r.X+S(143),r.Y+S(80),r.Width-S(156),S(23)),Muted,0.64f);
            Text(b,Game1.smallFont,unlocked?$"재고 {have:N0} · 최대 {max}배치":$"잠금: {reason}",new Rectangle(r.X+S(143),r.Y+S(106),r.Width-S(156),S(23)),unlocked?Ink:Red,0.61f);
        }
    }

    private void Detail(SpriteBatch b)
    {
        Panel(b,D(785,125,475,515)); Plaque(b,D(804,123,437,37),"선택 제품 상세",0.80f);
        ProductionRecipeDefinition? recipe=Mod.Production.FindRecipe(SelectedKey)??Rows().FirstOrDefault(); if(recipe is null)return; SelectedKey=recipe.Key;
        bool unlocked=Mod.Production.IsRecipeUnlocked(recipe,out string reason);
        DrawProduct(b,recipe,D(943,176,160,160),unlocked?1f:0.38f);
        Text(b,Game1.dialogueFont,recipe.DisplayName,D(818,334,410,43),unlocked?Ink:Muted,0.90f,true);
        Text(b,Game1.smallFont,string.Equals(recipe.OutputKind,"Intermediate",StringComparison.OrdinalIgnoreCase)?"중간재":"완제품",D(921,373,205,25),Green,0.78f,true);
        string ing=Mod.Production.GetIngredientDisplayName(recipe); int have=Mod.Production.GetIngredientQuantity(recipe); ProductionForecast fc=Mod.Quality.GetForecast(recipe,1);
        DetailRow(b,"필요 재료",$"{ing} × {recipe.InputQuantity}",409); DetailRow(b,"현재 재고",have.ToString("N0"),440); DetailRow(b,"생산 라인",$"{LineName(recipe.LineType)} 라인",471); DetailRow(b,"예상 시간",ProductionCore.FormatDuration(recipe.DurationMinutes),502); DetailRow(b,"예상 생산량",$"{fc.MinOutput} ~ {fc.MaxOutput}{recipe.OutputUnit}",533); DetailRow(b,"예상 등급",fc.MostLikelyGrade,564);
        Text(b,Game1.smallFont,unlocked?$"해금: 회사 Lv.{recipe.RequiredCompanyLevel} · 브랜드 {recipe.RequiredBrandPoints}":reason,D(820,592,405,24),unlocked?GreenDeep:Red,0.67f,true);
        Button(b,OneBatch(),"1배치 생산",unlocked,unlocked?Green:new Color(133,121,97)); Button(b,MaxBatch(),"최대 생산",false,unlocked?Blue:new Color(133,121,97));
    }

    private void DetailRow(SpriteBatch b,string label,string value,int y){Text(b,Game1.smallFont,label,D(816,y,110,24),Ink,0.72f);Dots(b,D(928,y+12,135,2));Text(b,Game1.smallFont,value,D(1070,y,165,24),Ink,0.70f);}
    private void Footer(SpriteBatch b){List<ProductionRecipeDefinition> rows=Rows();int max=Math.Max(0,(rows.Count-1)/6);Button(b,Prev(),"이전",false);Button(b,Next(),"다음",false);Text(b,Game1.smallFont,$"{rows.Count}개 레시피 · {Page+1}/{max+1}",D(510,650,260,25),Ink,0.72f,true);Text(b,Game1.smallFont,Message,D(265,690,750,18),Muted,0.58f,true);}

    private Rectangle Back()=>D(22,17,175,43); private Rectangle Close()=>D(1217,14,45,45); private Rectangle FilterBtn(int i)=>D(44+i*140,83,126,31); private Rectangle RecipeCard(int i){int col=i%2,row=i/2;return D(40+col*363,170+row*154,345,140);} private Rectangle OneBatch()=>D(817,608,175,39); private Rectangle MaxBatch()=>D(1004,608,175,39); private Rectangle Prev()=>D(38,649,108,34); private Rectangle Next()=>D(1134,649,108,34);
}
