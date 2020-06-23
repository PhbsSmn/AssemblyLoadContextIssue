namespace Alc.Services.Root
{
    public class SharedService : ISharedService
    {
        #region Fields
        private int _counter;
        #endregion

        #region Constructors
        public SharedService()
        {
            _counter = 0;
        }
        #endregion

        #region Methods
        public int SharedCallAmount()
        {
            return ++_counter;
        }
        #endregion
    }
}
