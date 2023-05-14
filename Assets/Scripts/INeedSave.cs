// 用于搜索当前所有需要保存数据的脚本，并调用其方法
public interface INeedSave
{
    public void SaveData(ref GameData gd);
    public void LoadData(GameData gd);
}
