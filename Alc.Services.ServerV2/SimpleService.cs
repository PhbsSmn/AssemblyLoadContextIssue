namespace Alc.Services.ServerV2
{
    public class SimpleService: ISimpleService
    {
        private readonly ISharedService _sharedService;

        public SimpleService(ISharedService sharedService)
        {
            _sharedService = sharedService;
        }

        public string LibraryVersion()
        {
            return $"Shared count: {_sharedService.SharedCallAmount()}, version: {SimpleDemoLibrary.VersionRetriever.GetVersion}";
        }
    }
}