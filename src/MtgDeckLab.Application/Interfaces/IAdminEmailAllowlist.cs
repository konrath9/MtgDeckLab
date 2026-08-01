namespace MtgDeckLab.Application.Interfaces;

public interface IAdminEmailAllowlist
{
    bool IsAdmin(string email);
}
