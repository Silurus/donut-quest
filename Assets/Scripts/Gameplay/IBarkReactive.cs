namespace DonutQuest.Gameplay
{
    /// <summary>
    /// Всё, что умеет реагировать на лай Корры.
    /// Реализуют: кот, птицы, сосед.
    /// </summary>
    public interface IBarkReactive
    {
        /// <param name="source">Transform Корры — чтобы реакция могла учесть направление.</param>
        void OnBark(UnityEngine.Transform source);
    }
}
