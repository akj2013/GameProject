namespace WoodLand3D.Resources.Types
{
    /// <summary>
    /// 자원 타입에 필요한 도구를 반환한다. 자동 채집·툴 표시에 사용.
    /// </summary>
    public static class ResourceToolMapping
    {
        /// <summary>
        /// 해당 자원을 채집할 때 필요한 도구 타입.
        /// </summary>
        public static ToolType GetToolForResource(ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Tree:           return ToolType.Axe;
                case ResourceType.Rock:
                case ResourceType.Ore:           return ToolType.Pickaxe;
                case ResourceType.Rice:          return ToolType.Sickle;
                case ResourceType.Potato:
                case ResourceType.SweetPotato:   return ToolType.Hoe;
                default:                        return ToolType.None;
            }
        }
    }
}
