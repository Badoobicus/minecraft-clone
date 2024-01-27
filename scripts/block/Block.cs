public class Block
{
    public string Id
    {
        get => this.id;
    }
    private string id;

    public Block(string id)
    {
        this.id = id;
    }

    public override bool Equals(object obj)
    {
        return obj != null && obj.GetType() == this.GetType() && ((Block)obj).id == this.id;
    }

    public override int GetHashCode()
    {
        return this.id.GetHashCode();
    }

    public override string ToString()
    {
        return this.id;
    }
}
