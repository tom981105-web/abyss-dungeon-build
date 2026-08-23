namespace AgriculturalCompany;

public sealed class VanillaProductDefinition
{
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string ItemId { get; set; } = "";
    public string IngredientName { get; set; } = "";
    public string ProductFamily { get; set; } = "VanillaVegetable";
    public string Style { get; set; } = "Pickle";
    public int InputQuantity { get; set; } = 5;
    public int OutputQuantity { get; set; } = 3;
    public int RequiredCompanyLevel { get; set; } = 1;
    public int RequiredBrandPoints { get; set; }
}

internal static class VanillaProductionCatalog
{
    internal static List<ProductionRecipeDefinition> Build(IEnumerable<VanillaProductDefinition>? definitions)
    {
        List<ProductionRecipeDefinition> result = new();
        if (definitions is null)
            return result;

        foreach (VanillaProductDefinition def in definitions)
        {
            if (string.IsNullOrWhiteSpace(def.Key) || string.IsNullOrWhiteSpace(def.ItemId))
                continue;

            List<ProductionStageDefinition> stages = BuildStages(def.Style);
            string outputKind = string.Equals(def.Style, "Research", StringComparison.OrdinalIgnoreCase) ? "Intermediate" : "Finished";
            string outputIntermediateKey = outputKind == "Intermediate" ? "QiResearchSample" : "";
            string outputIntermediateName = outputKind == "Intermediate" ? "치 연구 샘플" : "";

            result.Add(new ProductionRecipeDefinition
            {
                Key = def.Key,
                DisplayName = def.DisplayName,
                Description = BuildDescription(def),
                IngredientItemId = def.ItemId,
                IngredientFamily = "",
                IngredientIntermediateKey = "",
                IngredientDisplayName = def.IngredientName,
                OutputKind = outputKind,
                OutputIntermediateKey = outputIntermediateKey,
                OutputIntermediateDisplayName = outputIntermediateName,
                ProductFamily = def.ProductFamily,
                InputQuantity = Math.Max(1, def.InputQuantity),
                OutputQuantity = Math.Max(1, def.OutputQuantity),
                DurationMinutes = stages.Sum(p => Math.Max(10, p.DurationMinutes)),
                RequiredCompanyLevel = Math.Max(1, def.RequiredCompanyLevel),
                RequiredBrandPoints = Math.Max(0, def.RequiredBrandPoints),
                RequiresCropGenetics = false,
                LineType = GetLineType(def.Style),
                OutputUnit = GetOutputUnit(def),
                Stages = stages
            });
        }

        return result;
    }

    private static string BuildDescription(VanillaProductDefinition def)
    {
        if (string.Equals(def.Style, "Research", StringComparison.OrdinalIgnoreCase))
            return "치 열매는 기간 한정 퀘스트 작물이므로 판매용 상품 대신 사내 연구 샘플로만 처리합니다.";
        if (string.Equals(def.Style, "Bouquet", StringComparison.OrdinalIgnoreCase))
            return $"{def.IngredientName}을 선별·손질해 농업회사 선물용 {def.DisplayName}으로 포장합니다.";
        if (string.Equals(def.Style, "Grain", StringComparison.OrdinalIgnoreCase))
            return $"{def.IngredientName}을 선별·건조·가공해 장기 보관 가능한 {def.DisplayName}으로 만듭니다.";
        return $"{def.IngredientName}을 회사 표준 공정으로 가공해 {def.DisplayName}으로 생산합니다.";
    }

    private static string GetLineType(string style)
        => style switch
        {
            "Bouquet" or "Gift" or "BeveragePack" or "Grain" or "Research" => "Packaging",
            "Pickle" or "Preserve" => "Fermentation",
            _ => "Beverage"
        };

    private static string GetOutputUnit(VanillaProductDefinition def)
        => def.Style switch
        {
            "Bouquet" => "부케",
            "Gift" => "상자",
            "Juice" or "Extract" or "Preserve" => "병",
            "Cooked" when def.DisplayName.Contains("소스", StringComparison.CurrentCulture) => "병",
            "Research" => "샘플",
            _ => "팩"
        };

    private static List<ProductionStageDefinition> BuildStages(string style)
        => style switch
        {
            "Bouquet" => Stages(("select", "선별", 35), ("trim", "정리", 25), ("bundle", "부케 구성", 35), ("pack", "포장", 25)),
            "Pickle" => Stages(("wash", "세척", 30), ("cut", "정선·절단", 35), ("brine", "절임·가공", 55), ("pack", "포장", 30)),
            "Cooked" => Stages(("wash", "세척", 30), ("prep", "전처리", 40), ("process", "가열·가공", 60), ("pack", "포장", 30)),
            "Grain" => Stages(("clean", "선별", 30), ("dry", "건조", 40), ("mill", "제분·정미", 60), ("pack", "포장", 30)),
            "BeveragePack" => Stages(("sort", "선별", 25), ("dry", "건조", 45), ("roast", "로스팅·가공", 55), ("pack", "포장", 30)),
            "Extract" => Stages(("wash", "선별·세척", 30), ("extract", "추출", 65), ("filter", "여과", 40), ("bottle", "병입", 30)),
            "Preserve" => Stages(("wash", "세척", 25), ("cook", "저온 조리", 65), ("reduce", "농축", 45), ("jar", "병입", 30)),
            "Gift" => Stages(("select", "프리미엄 선별", 45), ("inspect", "품질검사", 40), ("cushion", "완충포장", 45), ("box", "세트 포장", 35)),
            "Research" => Stages(("inspect", "검수", 35), ("sample", "샘플링", 35), ("seal", "밀봉", 30)),
            _ => Stages(("wash", "세척", 30), ("press", "착즙", 55), ("filter", "여과", 35), ("sterilize", "살균", 40), ("bottle", "병입", 30))
        };

    private static List<ProductionStageDefinition> Stages(params (string Key, string Name, int Minutes)[] values)
        => values.Select(p => new ProductionStageDefinition
        {
            Key = p.Key,
            DisplayName = p.Name,
            DurationMinutes = p.Minutes
        }).ToList();
}
