using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace AgriculturalCompany;

internal sealed class Production091Menu : Company091UiBase
{
    private string SelectedRecipeKey = "";
    private int PlanPage;
    private string Message = "생산 계획을 등록하면 빈 라인에 자동으로 배정됩니다.";

    internal Production091Menu(ModEntry mod) : base(mod)
    {
        Mod.Production.EnsureState();
        SelectedRecipeKey = Mod.Recipes.FirstOrDefault(p => string.Equals(p.Key, "TomatoJuice", StringComparison.OrdinalIgnoreCase))?.Key
            ?? Mod.Recipes.FirstOrDefault(p => !p.RequiresCropGenetics)?.Key
            ?? Mod.Recipes.FirstOrDefault()?.Key ?? "";
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (Close().Contains(x, y)) { Game1.playSound("bigDeSelect"); exitThisMenu(); return; }
        if (Company().Contains(x, y)) { Game1.playSound("bigDeSelect"); Game1.activeClickableMenu = new CompanyMenu(Mod); return; }
        if (Catalog().Contains(x, y) || PlanAdd().Contains(x, y)) { Game1.playSound("bigSelect"); Game1.activeClickableMenu = new ProductCatalog091Menu(Mod, SelectedRecipeKey); return; }

        IReadOnlyList<ProductionLineState> lines = Mod.Production.GetLines();
        for (int i = 0; i < Math.Min(3, lines.Count); i++)
        {
            if (!LineCard(i).Contains(x, y)) continue;
            ProductionJob? job = Mod.Production.GetLineJob(lines[i].Id);
            ProductionRecipeDefinition? recipe = job is null
                ? Mod.Recipes.FirstOrDefault(p => string.Equals(p.LineType, lines[i].LineType, StringComparison.OrdinalIgnoreCase))
                : Mod.Production.FindRecipe(job.RecipeKey);
            if (recipe is not null) SelectedRecipeKey = recipe.Key;
            Game1.playSound("smallSelect"); return;
        }

        ProductionRecipeDefinition? selected = Mod.Production.FindRecipe(SelectedRecipeKey);
        if (selected is not null && OneBatch().Contains(x, y))
        {
            bool ok = Mod.Production.TryStart(selected.Key, 1, out string m); Message = m; Game1.playSound(ok ? "Ship" : "cancel"); return;
        }
        if (selected is not null && MaxBatch().Contains(x, y))
        {
            int max = Mod.Production.GetMaxBatches(selected);
            if (max <= 0) { Message = $"{Mod.Production.GetIngredientDisplayName(selected)} 재고가 부족합니다."; Game1.playSound("cancel"); return; }
            bool ok = Mod.Production.TryStart(selected.Key, Math.Min(10, max), out string m); Message = m; Game1.playSound(ok ? "Ship" : "cancel"); return;
        }

        List<ProductionPlanEntry> plans = Mod.Production.GetPlans().ToList();
        int start = PlanPage * 5;
        for (int row = 0; row < 5; row++)
        {
            int idx = start + row;
            if (idx >= plans.Count || !PlanRow(row).Contains(x, y)) continue;
            ProductionPlanEntry plan = plans[idx]; SelectedRecipeKey = plan.RecipeKey;
            if (PlanUp(row).Contains(x, y)) { bool ok = Mod.Production.TryMovePlan(plan.Id, -1, out string m); Message = m; Game1.playSound(ok ? "shiny4" : "cancel"); }
            else if (PlanDown(row).Contains(x, y)) { bool ok = Mod.Production.TryMovePlan(plan.Id, 1, out string m); Message = m; Game1.playSound(ok ? "shiny4" : "cancel"); }
            else if (PlanRemove(row).Contains(x, y)) { bool ok = Mod.Production.TryRemovePlan(plan.Id, out string m); Message = m; Game1.playSound(ok ? "trashcan" : "cancel"); }
            else Game1.playSound("smallSelect");
            return;
        }
    }

    public override void receiveRightClick(int x, int y, bool playSound = true)
    {
        List<ProductionPlanEntry> plans = Mod.Production.GetPlans().ToList(); int start = PlanPage * 5;
        for (int row = 0; row < 5; row++)
        {
            int idx = start + row; if (idx >= plans.Count || !PlanRow(row).Contains(x, y)) continue;
            bool ok = Mod.Production.TryRemovePlan(plans[idx].Id, out string m); Message = m; Game1.playSound(ok ? "trashcan" : "cancel"); return;
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        int max = Math.Max(0, (Mod.Production.GetPlans().Count - 1) / 5);
        if (direction < 0 && PlanPage < max) PlanPage++; else if (direction > 0 && PlanPage > 0) PlanPage--;
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.70f);
        Frame(b); Header(b); Stats(b); Lines(b); Current(b); Plans(b); Bottom(b);
        if (!string.IsNullOrWhiteSpace(Message)) Text(b, Game1.smallFont, Message, D(340, 704, 600, 13), new Color(88,58,31), 0.56f, true);
        drawMouse(b);
    }

    private void Header(SpriteBatch b)
    {
        Button(b, Company(), CompanyName(), false);
        Plaque(b, D(318, 9, 644, 57), $"{CompanyName()} · 생산 관리 2.0", 1.08f);
        Button(b, Close(), "X", false, new Color(188,75,44));
    }

    private void Stats(SpriteBatch b)
    {
        Stat(b, D(20,82,296,67), 0, "회사 자금", $"{Mod.State.CompanyFunds:N0}G");
        Stat(b, D(327,82,296,67), 1, "브랜드", Mod.Brand.GetTierName(Mod.State.BrandPoints));
        Stat(b, D(634,82,296,67), 2, "활성 계약", $"{Mod.State.AcceptedContracts.Count}건");
        Stat(b, D(941,82,319,67), 3, "평판", Mod.State.Reputation.ToString("N0"));
    }

    private void Stat(SpriteBatch b, Rectangle r, int icon, string label, string value)
    {
        Card(b, r); Rectangle ir = new(r.X+S(15), r.Y+S(11), S(46), S(46));
        if (icon==0) Coin(b,ir); else if (icon==1) Shield(b,ir); else if (icon==2) ScrollIcon(b,ir); else Heart(b,ir);
        Text(b,Game1.smallFont,label,new Rectangle(r.X+S(75),r.Y+S(7),r.Width-S(88),S(25)),Ink,0.90f);
        Text(b,Game1.dialogueFont,value,new Rectangle(r.X+S(75),r.Y+S(28),r.Width-S(88),S(33)),Ink,0.76f);
    }

    private void Lines(SpriteBatch b)
    {
        Panel(b,D(20,161,340,420)); Plaque(b,D(38,159,304,38),"생산 라인",0.80f);
        IReadOnlyList<ProductionLineState> lines=Mod.Production.GetLines();
        for(int i=0;i<3;i++)
        {
            Rectangle card=LineCard(i); Card(b,card);
            if(i>=lines.Count){Text(b,Game1.smallFont,$"라인 {i+1} · 잠김",new Rectangle(card.X+S(14),card.Y+S(8),card.Width-S(28),S(28)),Muted,0.86f);continue;}
            ProductionLineState line=lines[i]; ProductionJob? job=Mod.Production.GetLineJob(line.Id); ProductionRecipeDefinition? recipe=job is null?null:Mod.Production.FindRecipe(job.RecipeKey);
            Text(b,Game1.smallFont,$"라인 {i+1} · {LineName(line.LineType)}",new Rectangle(card.X+S(12),card.Y+S(5),S(190),S(25)),Ink,0.91f);
            Status(b,new Rectangle(card.Right-S(69),card.Y+S(5),S(58),S(25)),job is null?"대기":"가동",job is not null);
            DrawAtlas(b,MachineSprite(line.LineType),new Rectangle(card.X+S(7),card.Y+S(31),S(151),S(91)),job is null?0.84f:1f);
            if(recipe is not null) DrawProduct(b,recipe,new Rectangle(card.X+S(161),card.Y+S(34),S(52),S(52)));
            Text(b,Game1.smallFont,recipe?.DisplayName??"대기 중",new Rectangle(card.X+S(218),card.Y+S(31),card.Width-S(228),S(29)),Ink,0.89f);
            string stage=job is null?"작업 없음":Mod.Production.GetCurrentStageName(job); Text(b,Game1.smallFont,$"현재 단계 {stage}",new Rectangle(card.X+S(161),card.Y+S(61),card.Width-S(172),S(24)),job is null?Muted:GreenDeep,0.72f);
            float p=job is null?0f:(float)Mod.Production.GetJobProgress(job); Progress(b,new Rectangle(card.X+S(161),card.Y+S(88),S(127),S(13)),p);
            Text(b,Game1.smallFont,$"{Math.Clamp((int)(p*100),0,100)}%",new Rectangle(card.X+S(292),card.Y+S(82),S(36),S(24)),Ink,0.69f,true);
            int eff=job?.EfficiencyPercent??Mod.Production.GetLineEfficiency(line); string remain=job is null?"-":ProductionCore.FormatDuration(job.RemainingMinutes);
            Text(b,Game1.smallFont,$"시간 {remain}",new Rectangle(card.X+S(161),card.Y+S(107),S(91),S(22)),Ink,0.61f);
            Text(b,Game1.smallFont,$"효율 {eff}%",new Rectangle(card.X+S(249),card.Y+S(107),S(79),S(22)),Green,0.61f,true);
        }
        Button(b,D(38,552,304,25),"작업 배정",false);
    }

    private void Current(SpriteBatch b)
    {
        Panel(b,D(370,161,560,420)); Plaque(b,D(390,159,520,38),"현재 생산 상세",0.80f);
        ProductionRecipeDefinition? recipe=Mod.Production.FindRecipe(SelectedRecipeKey)??Mod.Recipes.FirstOrDefault(); if(recipe is null)return;
        ProductionJob? active=Mod.State.ProductionQueue.FirstOrDefault(p=>string.Equals(p.RecipeKey,recipe.Key,StringComparison.OrdinalIgnoreCase)); ProductionForecast fc=active is null?Mod.Quality.GetForecast(recipe,1):Mod.Quality.GetForecast(active);
        DrawProduct(b,recipe,D(544,202,82,82)); Text(b,Game1.dialogueFont,recipe.DisplayName,D(638,207,230,38),Ink,0.85f); Text(b,Game1.smallFont,string.Equals(recipe.OutputKind,"Intermediate",StringComparison.OrdinalIgnoreCase)?"중간재":"완제품",D(640,244,100,22),Green,0.70f);
        Flow(b,recipe,active);
        Card(b,D(394,423,320,104)); Metric(b,"진행률",$"{Math.Clamp((int)((active is null?0f:(float)Mod.Production.GetJobProgress(active))*100),0,100)}%",433); Metric(b,"예상 생산량",$"{fc.MinOutput} ~ {fc.MaxOutput}{recipe.OutputUnit}",456); Metric(b,"예상 등급",fc.MostLikelyGrade,479); Metric(b,"예상 시간",ProductionCore.FormatDuration(active?.RemainingMinutes??recipe.DurationMinutes),502);
        Card(b,D(724,423,182,104)); Text(b,Game1.smallFont,"품질 요약",D(730,426,170,23),Ink,0.80f,true); GradeChance(b,"S",fc.SChance,738,452,Gold); GradeChance(b,"A",fc.AChance,818,452,GreenBright); GradeChance(b,"B",fc.BChance,738,488,Blue); GradeChance(b,"C",fc.CChance,818,488,new Color(188,116,57));
        Button(b,OneBatch(),"+ 1배치 추가",true); Button(b,MaxBatch(),"최대 생산",false,Blue); Button(b,Catalog(),"제품 카탈로그",false);
    }

    private void Flow(SpriteBatch b, ProductionRecipeDefinition recipe, ProductionJob? active)
    {
        List<(string name,int idx)> nodes=new(){("원재료",-1)}; foreach(var stage in recipe.Stages.Take(4).Select((s,i)=>(s.DisplayName,i)))nodes.Add(stage); nodes.Add(("완제품",99));
        int n=nodes.Count; float total=500f,gap=8f,nodeW=(total-gap*(n-1))/n;
        for(int i=0;i<n;i++)
        {
            int dx=400+(int)MathF.Round(i*(nodeW+gap)); bool current=active is not null&&nodes[i].idx>=0&&nodes[i].idx<99&&active.CurrentStageIndex==nodes[i].idx; Rectangle card=D(dx,286,(int)nodeW,124);
            if(current){Fill(b,D(dx-3,283,(int)nodeW+6,130),Orange);Card(b,card,new Color(255,220,165));}else Card(b,card);
            Rectangle art=D(dx+Math.Max(2,((int)nodeW-68)/2),299,68,74); if(nodes[i].idx==-1)DrawAtlas(b,3,art);else if(nodes[i].idx==99)DrawProduct(b,recipe,art);else DrawAtlas(b,ProcessSprite(nodes[i].name),art,current?1f:0.95f);
            Text(b,Game1.smallFont,nodes[i].name,D(dx+2,376,(int)nodeW-4,28),current?GreenDeep:Ink,0.64f,true); if(i<n-1)Arrow(b,D(dx+(int)nodeW+1,337,(int)gap+8,18));
        }
    }

    private void Plans(SpriteBatch b)
    {
        Panel(b,D(940,161,320,420)); Plaque(b,D(958,159,284,38),"생산 계획",0.80f); List<ProductionPlanEntry> plans=Mod.Production.GetPlans().ToList(); int start=PlanPage*5;
        for(int row=0;row<5;row++)
        {
            Rectangle r=PlanRow(row); Card(b,r); Rectangle number=new(r.X,r.Y,S(42),r.Height); Fill(b,number,GreenDeep); Text(b,Game1.dialogueFont,(start+row+1).ToString(),number,Color.White,0.72f,true); int idx=start+row;
            if(idx>=plans.Count){Text(b,Game1.smallFont,"빈 계획",new Rectangle(r.X+S(58),r.Y,S(155),r.Height),Muted,0.77f);continue;}
            ProductionPlanEntry plan=plans[idx]; ProductionRecipeDefinition? recipe=Mod.Production.FindRecipe(plan.RecipeKey); if(recipe is not null)DrawProduct(b,recipe,new Rectangle(r.X+S(49),r.Y+S(7),S(45),S(45)));
            Text(b,Game1.smallFont,$"{recipe?.DisplayName??plan.RecipeKey} × {plan.BatchCount}",new Rectangle(r.X+S(100),r.Y+S(5),S(130),S(47)),Ink,0.70f); ArrowButton(b,PlanUp(row),true); ArrowButton(b,PlanDown(row),false); Fill(b,PlanRemove(row),Red); Text(b,Game1.smallFont,"X",PlanRemove(row),Color.White,0.52f,true);
        }
        Button(b,PlanAdd(),"+ 계획 추가",false); Text(b,Game1.smallFont,$"자동 배정 ON   {PlanPage+1}/{Math.Max(1,(plans.Count+4)/5)}",D(970,558,260,17),Ink,0.60f,true);
    }

    private void Bottom(SpriteBatch b){StockPanel(b,D(20,598,615,105),"중간재",false);StockPanel(b,D(645,598,615,105),"완제품",true);}

    private void StockPanel(SpriteBatch b, Rectangle r, string title, bool finished)
    {
        Panel(b,r); Tile(b,new Rectangle(r.X+S(4),r.Y+S(4),r.Width-S(8),S(25)),0,Color.White*0.78f); Text(b,Game1.dialogueFont,title,new Rectangle(r.X,r.Y,r.Width,S(29)),new Color(249,220,148),0.60f,true);
        if(!finished)
        {
            List<IntermediateStockEntry> rows=Mod.Production.GetIntermediateStock().Where(p=>p.Quantity>0).Take(5).ToList(); for(int i=0;i<5;i++){Rectangle slot=new(r.X+S(12+i*119),r.Y+S(38),S(108),S(55));Card(b,slot);if(i>=rows.Count){Text(b,Game1.smallFont,"빈 슬롯",slot,Muted,0.56f,true);continue;}IntermediateStockEntry row=rows[i];Mod.Icons.DrawProductIcon(b,row.Key,new Rectangle(slot.X+S(6),slot.Y+S(7),S(39),S(39)));Text(b,Game1.smallFont,row.DisplayName,new Rectangle(slot.X+S(49),slot.Y+S(4),slot.Width-S(53),S(23)),Ink,0.52f);Text(b,Game1.smallFont,row.Quantity.ToString("N0"),new Rectangle(slot.X+S(49),slot.Y+S(28),slot.Width-S(53),S(20)),Ink,0.61f);}
        }
        else
        {
            List<ProductStockEntry> rows=Mod.State.FinishedGoods.Values.Where(p=>p is not null&&p.Quantity>0).OrderByDescending(p=>p.Quality).ThenByDescending(p=>p.Quantity).Take(5).ToList(); for(int i=0;i<5;i++){Rectangle slot=new(r.X+S(12+i*119),r.Y+S(38),S(108),S(55));Card(b,slot);if(i>=rows.Count){Text(b,Game1.smallFont,"빈 슬롯",slot,Muted,0.56f,true);continue;}ProductStockEntry row=rows[i];ProductionRecipeDefinition? recipe=Mod.Production.FindRecipe(row.ProductKey);if(recipe is not null)DrawProduct(b,recipe,new Rectangle(slot.X+S(5),slot.Y+S(5),S(42),S(42)));else Mod.Icons.DrawProductIcon(b,row.ProductKey,new Rectangle(slot.X+S(5),slot.Y+S(5),S(42),S(42)));Text(b,Game1.smallFont,recipe?.DisplayName??row.ProductKey,new Rectangle(slot.X+S(50),slot.Y+S(3),slot.Width-S(54),S(20)),Ink,0.50f);Grade(b,new Rectangle(slot.X+S(50),slot.Y+S(26),S(42),S(21)),row.Grade);Text(b,Game1.smallFont,row.Quantity.ToString("N0"),new Rectangle(slot.Right-S(27),slot.Y+S(28),S(22),S(18)),Ink,0.56f,true);}
        }
    }

    private void Metric(SpriteBatch b,string label,string value,int y){Text(b,Game1.smallFont,label,D(407,y,105,22),Ink,0.70f);Dots(b,D(512,y+11,88,2));Text(b,Game1.smallFont,value,D(605,y,98,22),Ink,0.71f,true);}
    private void GradeChance(SpriteBatch b,string grade,int chance,int x,int y,Color color){Star(b,D(x,y,19,19),color);Text(b,Game1.smallFont,$"{grade}급 {chance}%",D(x+24,y-1,54,22),Ink,0.58f);}
    private void ArrowButton(SpriteBatch b,Rectangle r,bool up){Card(b,r,new Color(238,211,163));int cx=r.X+r.Width/2,cy=r.Y+r.Height/2;Color c=new(104,73,40);Fill(b,new Rectangle(cx-S(2),up?cy-S(1):cy-S(6),S(4),S(8)),c);for(int i=0;i<S(8);i++){int w=Math.Max(1,S(14)-i*2);int yy=up?cy-S(8)+i:cy+S(1)+i;Fill(b,new Rectangle(cx-w/2,yy,w,1),c);}}

    private Rectangle Company()=>D(18,13,220,50); private Rectangle Close()=>D(1217,14,45,45); private Rectangle LineCard(int i)=>D(35,199+i*117,310,111); private Rectangle OneBatch()=>D(399,538,156,39); private Rectangle MaxBatch()=>D(564,538,156,39); private Rectangle Catalog()=>D(729,538,172,39); private Rectangle PlanRow(int row)=>D(955,200+row*62,290,54); private Rectangle PlanUp(int row)=>D(1174,205+row*62,27,21); private Rectangle PlanDown(int row)=>D(1174,229+row*62,27,21); private Rectangle PlanRemove(int row)=>D(1210,227+row*62,20,20); private Rectangle PlanAdd()=>D(958,530,284,36);
}
