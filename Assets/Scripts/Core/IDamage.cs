// anything that can be hurt implements this, so Damage does not care what it hit
public interface IDamage
{
    void TakeDamage(int amount);
}