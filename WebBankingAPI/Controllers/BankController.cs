using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebBankingAPI.Models;

namespace WebBankingAPI.Controllers
{
    public class BankController : Controller
    { 
        [Authorize]
        [HttpGet]
        [Route("/conti-correnti")]
        public ActionResult GetBankAccount()
        {
            using (WebBankingContext model = new WebBankingContext())
            {

                if (HttpContext.User.Claims.FirstOrDefault(x => x.Type == "IsBanker").Value == "False")
                {
                    int IdUser = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "Id").Value);
                    return Ok(model.BankAccounts.Where(w => w.FkUser == IdUser).ToList());
                }
                else
                {
                    return Ok(model.BankAccounts.ToList());
                }

            }
        }

        [Authorize]
        [HttpGet]
        [Route("/conti-correnti/{Id_}")]
        public ActionResult GetOneBankAccount(int Id_)
        {
            using (WebBankingContext model = new WebBankingContext())
            {

                if (HttpContext.User.Claims.FirstOrDefault(x => x.Type == "IsBanker").Value == "False")
                {
                    int IdUser = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "Id").Value);
                    if (model.BankAccounts.FirstOrDefault(q => q.Id == Id_ && q.FkUser == IdUser) != null)
                        return Ok(model.BankAccounts.Where(i => i.Id == Id_).ToList());
                    else
                        return Ok("PUOI VISUALIZZARE SOLO IL TUO CONTO BANCARIO");
                }
                else
                {
                    return Ok(model.BankAccounts.FirstOrDefault(q => q.Id == Id_));
                }

            }
        }

        [Authorize]
        [HttpGet]
        [Route("/conti-correnti/{IdBankAccount}/movimenti")]
        public ActionResult GetMoveBankAccount(int IdBankAccount)
        {
            using (WebBankingContext model = new WebBankingContext())
            {
                var MoveBA = model.AccountMovements.Where(o => o.FkBankAccount == IdBankAccount)
                .Select(o => new { o.Id, o.Date, o.FkBankAccount, o.In, o.Out, o.Description }).ToList();
                if (HttpContext.User.Claims.FirstOrDefault(x => x.Type == "IsBanker").Value == "False")
                {
                    int IdUser = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "Id").Value);
                    if (model.BankAccounts.FirstOrDefault(q => q.Id == IdBankAccount && q.FkUser == IdUser) != null)
                    {
                        return Ok(MoveBA);
                    }
                    else
                        return Ok("NON PUOI VEDERE I MOVIMENTI DI UN CONTO CHE NON E' TUO");
                }
                else
                    return Ok(model.AccountMovements.Where(q => q.FkBankAccount == IdBankAccount).OrderBy(b => b.Date).ToList());


            }
        }

        [Authorize]
        [HttpGet]
        [RouteAttribute("/conti-correnti/{IdBankAccount}/movimenti/{IdMove}")]
        public ActionResult GetOneMoveBankAccount(int IdBankAccount, int IdMove)
        {
            using (WebBankingContext model = new WebBankingContext())
            {
                var MoveBA = model.AccountMovements.Where(o => o.FkBankAccount == IdBankAccount && o.Id == IdMove)
                        .Select(o => new { o.Id, o.Date, o.FkBankAccount, o.In, o.Out, o.Description }).ToList();
                if (HttpContext.User.Claims.FirstOrDefault(x => x.Type == "IsBanker").Value == "False")
                {
                    int IdUser = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "Id").Value);
                    if (model.AccountMovements.FirstOrDefault(q => q.FkBankAccount == IdBankAccount && q.Id == IdMove) != null)
                    {
                        return Ok(MoveBA);
                    }
                    else
                        return Ok("NON PUOI VEDERE I MOVIMENTI DI UN CONTO CHE NON E' TUO");
                }
                else
                    if (model.AccountMovements.FirstOrDefault(q => q.FkBankAccount == IdBankAccount && q.Id == IdMove) != null)
                    return Ok(MoveBA);
                else
                    return Ok("L'ID DELLA TRANSIZIONE NON CONCIDE CON NESSUN MOVIMENTO DEL CONTO BANCARIO");

            }
        }

        [Authorize]
        [HttpPost]
        [Route("/conti-correnti/{IdBankAccount}/bonifico")]
        public ActionResult PostTransferMoneyBankAccount([FromBody] NewTransferMoneyBankAccount TransferMoney, int IdBankAccount)
        {
            using(WebBankingContext model = new WebBankingContext())
            {
                if (HttpContext.User.Claims.FirstOrDefault(x => x.Type == "IsBanker").Value == "False")
                {
                    int IdUser = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "Id").Value);
                    if (model.BankAccounts.FirstOrDefault(q => q.Id == IdBankAccount && q.FkUser == IdUser) != null)
                    {
                        double MoneyIntoBankAccount = (double)((model.AccountMovements.Where(w => w.FkBankAccount == IdBankAccount).Sum(q => q.In)) -
                               (model.AccountMovements.Where(w => w.FkBankAccount == IdBankAccount).Sum(q => q.Out)));

                        if (MoneyIntoBankAccount >= TransferMoney.importo && (model.BankAccounts.FirstOrDefault(q => q.Iban == TransferMoney.iban) != null))
                        {
                            AccountMovement newTransferUser = new AccountMovement();
                            newTransferUser.Date = DateTime.Now;
                            newTransferUser.FkBankAccount = IdBankAccount;
                            newTransferUser.Out = TransferMoney.importo;
                            newTransferUser.In = null;
                            newTransferUser.Description = "Bonifico per l'iban  " + model.BankAccounts.FirstOrDefault(q => q.Iban == TransferMoney.iban).Iban;

                            model.AccountMovements.Add(newTransferUser);


                            newTransferUser.FkBankAccount = model.BankAccounts.FirstOrDefault(q => q.Iban == TransferMoney.iban).Id;
                            newTransferUser.In = TransferMoney.importo;
                            newTransferUser.Out = null;
                            newTransferUser.Description = "Bonifico da parte dell'iban " + model.BankAccounts.FirstOrDefault(q => q.Id == IdBankAccount).Iban;

                            model.AccountMovements.Add(newTransferUser);
                            model.SaveChanges();

                            return Ok("TRANSAZIONE ESEGUITA CON SUCCESSO");
                        }
                        else
                            return Ok("CONTROLLA CHE IL TUO SALDO ABBIA ABBASTANZA DENARO O DIGITA UN'IBAN ESISTENTE");
                    }
                    else
                        return Ok("NON PUOI VEDERE I MOVIMENTI DI UN CONTO CHE NON E' TUO");
                }
                else
                {
                    //banker transfer
                    double MoneyIntoBankAccount = (double)((model.AccountMovements.Where(w => w.FkBankAccount == IdBankAccount).Sum(q => q.In)) -
                               (model.AccountMovements.Where(w => w.FkBankAccount == IdBankAccount).Sum(q => q.Out)));

                    if (MoneyIntoBankAccount >= TransferMoney.importo && model.BankAccounts.FirstOrDefault(q => q.Iban == TransferMoney.iban) != null)
                    {
                        AccountMovement newTransferBanker = new AccountMovement();
                        newTransferBanker.Date = DateTime.Now;
                        newTransferBanker.FkBankAccount = IdBankAccount;
                        newTransferBanker.Out = TransferMoney.importo;
                        newTransferBanker.In = null;
                        newTransferBanker.Description = "Bonifico per l'iban  " + model.BankAccounts.FirstOrDefault(q => q.Iban == TransferMoney.iban).Iban;

                        model.AccountMovements.Add(newTransferBanker);
                        

                        newTransferBanker.FkBankAccount = model.BankAccounts.FirstOrDefault(q => q.Iban == TransferMoney.iban).Id;
                        newTransferBanker.In = TransferMoney.importo;
                        newTransferBanker.Out = null;
                        newTransferBanker.Description = "Bonifico da parte dell'iban " + model.BankAccounts.FirstOrDefault(q => q.Id == IdBankAccount).Iban;

                        model.AccountMovements.Add(newTransferBanker);
                        model.SaveChanges();

                        return Ok("BONIFICO ESEGUITO CON SUCCESSO");
                    }
                    else
                        return Ok("CONTROLLA IL TUO SALDO O L'INSERIMENTO DEL IBAN");
                }
            }
        }

        [Authorize]
        [HttpPost]
        [Route("/conti-correnti")]
        public ActionResult PostNewBankAccount([FromBody] BankAccount NewBankAccount)
        {
            if (HttpContext.User.Claims.FirstOrDefault(x => x.Type == "IsBanker").Value == "False")
                return Ok("NON PUOI ACCEDERE A QUESTA FUNZIONE");
            else
            {
                using(WebBankingContext model = new WebBankingContext())
                {
                    if (NewBankAccount.Iban != null || NewBankAccount.FkUser != null)
                        if (model.BankAccounts.Any(t=> t.Iban == NewBankAccount.Iban))
                            return Ok("IBAN GIA' ESISTENTE, CONTO NON CREATO");
                        else
                        {
                            model.BankAccounts.Add(NewBankAccount);
                            model.SaveChanges();
                            return Ok("CREAZIONE AVVENUTA CON SUCCESSO");
                        }
                    else
                        return Ok("CONTROLLA DI AVER INSERITO TUTTI I DATI CORRETTAMENTE");
                }
            }
        }

        [Authorize]
        [HttpPut]
        [Route("/conti-correnti/{IdBankAccount}")]
        public ActionResult PutModBankaccount(int IdBankAccount,[FromBody] BankAccount ModBankAccount)
        {
            if (HttpContext.User.Claims.FirstOrDefault(x => x.Type == "IsBanker").Value == "False")
                return Ok("NON PUOI ACCEDERE A QUESTA FUNZIONE");
            else
            {
                using (WebBankingContext model = new WebBankingContext())
                {
                    if (ModBankAccount.Iban != null || ModBankAccount.FkUser != null)
                        if (model.BankAccounts.Any(t => t.Iban == ModBankAccount.Iban))
                            return Ok("IBAN GIA' ESISTENTE, CONTO NON MODIFICATO");
                        else
                        {
                            model.BankAccounts.FirstOrDefault(q => q.Id == IdBankAccount).Iban = ModBankAccount.Iban;
                            model.BankAccounts.FirstOrDefault(q => q.Id == IdBankAccount).FkUser = ModBankAccount.FkUser;
                            model.SaveChanges();
                            return Ok("MODIFICA AVVENUTA CON SUCCESSO");
                        }
                    else
                        return Ok("CONTROLLA DI AVER INSERITO TUTTI I DATI CORRETTAMENTE");
                }
            }
        }

        [Authorize]
        [HttpDelete]
        [Route("/conti-correnti/{IdBankAccount}")]
        public ActionResult DeleteBankAccount(int IdBankAccount)
        {
            if (HttpContext.User.Claims.FirstOrDefault(x => x.Type == "IsBanker").Value == "False")
                return Ok("NON PUOI ACCEDERE A QUESTA FUNZIONE");
            else
            {
                using (WebBankingContext model = new WebBankingContext())
                {
                    if (model.BankAccounts.Any(t => t.Id == IdBankAccount))
                    {
                        BankAccount BankAccountRemove = model.BankAccounts.FirstOrDefault(q=> q.Id == IdBankAccount);
                        //BankAccount BankAccountRemove = model.BankAccounts.FirstOrDefault(q=> q.Id == IdBankAccount);
                        List<AccountMovement> AccountMovements = model.AccountMovements.Where(q => q.FkBankAccount == IdBankAccount).ToList();
                        
                        
                        model.AccountMovements.RemoveRange(AccountMovements);



                        model.BankAccounts.Remove(BankAccountRemove);

                        model.SaveChanges();
                        
                       

                        return Ok("CANCELLAZIONE AVVENUTA CON SUCCESSO");
                    }
                    else
                        return Ok("ID NON TROVATO");
                }
            }
        }
    }
}


