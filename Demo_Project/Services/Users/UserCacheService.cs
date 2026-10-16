using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace PayslipMgtSystem.Services.Users
{
    public class UserCacheService : CacheServiceBase
    {

        private const string SessionSecret = "UTKN";
        private const string SessionTime = "UTK_EXP";
        private const string SignalRHubConnnectionId = "UCTNID";

        private const string OrganizationId = "OrgId";

        private const string UserIpKey = "UserIp";

        public UserCacheService(IDistributedCache cache, ILogger<UserCacheService> logger) : base(cache, logger)
        {
        }

        #region Code
        public async Task<string> GetUserSessionSecret(string id)
        {
            return await GetString($"{SessionSecret}{id}");
        }

        public async Task SetUserSessioinSecret(string id, string code)
        {
            await SetString($"{SessionSecret}{id}", code);
        }

        #endregion

        #region Code
        public async Task<string> GetHubConnectionId(string id)
        {
            return await GetString($"{SignalRHubConnnectionId}{id}");
        }

        public async Task SetHubConnectionId(string id, string connectionId)
        {
            await SetString($"{SignalRHubConnnectionId}{id}", connectionId);
        }

        #endregion

        #region SessionTime
        public async Task<DateTime?> GetSessionTime(string id)
        {
            return await GetDateTime($"{SessionTime}{id}");
        }

        public async Task SetSessionTime(string id, DateTime dateTime)
        {
            await SetDateTime($"{SessionTime}{id}", dateTime);
        }

        #endregion

        #region UserOrgId
        public async Task<int> GetOrgId(string id)
        {
            return await GetInt($"{OrganizationId}{id}");
        }

        public async Task SetOrgId(string id, int orgId)
        {
            await SetInt($"{OrganizationId}{id}", orgId);
        }

        #endregion

        #region UserIp
        public async Task<string> GetUserIp(string userName)
        {
            return await GetString($"{UserIpKey}{userName}");
        }

        public async Task SetUserIp(string id, string userIp)
        {
            await SetString($"{UserIpKey}{id}", userIp);
        }

        #endregion
    }

    public class CacheServiceBase
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger _logger;


        public CacheServiceBase(IDistributedCache cache, ILogger logger)
        {
            _cache = cache;
            _logger = logger;
        }



        protected async Task<int> GetInt(string key)
        {
            try
            {
                var value = await _cache.GetStringAsync(key);
                return Convert.ToInt32(value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
                return 0;
            }
        }

        protected async Task SetInt(string key, int value)
        {
            try
            {
                await _cache.SetStringAsync(key, value.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
            }
        }

        protected async Task<string> GetString(string key, string defaultValue = null)
        {
            try
            {
                var value = await _cache.GetStringAsync(key);
                return value ?? defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
                return defaultValue;
            }
        }

        protected async Task<DateTime?> GetDateTime(string key)
        {
            try
            {
                var value = await _cache.GetStringAsync(key);
                return DateTime.ParseExact(value, "dd-MMM-yyyy HH:mm:ss", null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
                return null;
            }
        }

        protected async Task SetString(string key, string value)
        {
            try
            {
                if (value != null)
                    await _cache.SetStringAsync(key, value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
            }
        }

        protected async Task SetDateTime(string key, DateTime value)
        {
            try
            {
                await _cache.SetStringAsync(key, value.ToString("dd-MMM-yyyy HH:mm:ss"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
            }
        }
    }
}
