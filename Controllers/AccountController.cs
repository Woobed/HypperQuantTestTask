using HyperQuantTestTask.Connector;
using Microsoft.AspNetCore.Mvc;

namespace HyperQuantTestTask.Controllers
{
    public class AccountController : Controller
    {

        private AccountService _accountService;
        public AccountController(AccountService accountService)
        {
            _accountService = accountService;
        }

        public async Task<ActionResult> Index()
        {
            var balanceDataGrid = await _accountService.GetPortfolioBalanceAsync();
            return View(balanceDataGrid);
        }


    }
}
