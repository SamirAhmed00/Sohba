using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.DBInitializer
{
    public interface IDBInitializer
    {
        public Task InitializeAsync();
    }
}
