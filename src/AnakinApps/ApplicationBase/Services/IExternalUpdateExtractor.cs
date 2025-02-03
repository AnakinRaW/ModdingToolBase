using System.Threading.Tasks;

namespace AnakinRaW.ApplicationBase.Services;

internal interface IExternalUpdateExtractor
{
    Task ExtractAsync();
}