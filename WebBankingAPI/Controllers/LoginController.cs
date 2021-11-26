using WebBankingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebBankingAPI.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        [HttpPost("/login")]
        public ActionResult Login([FromBody] User credentials)
        {
            using (WebBankingContext model = new WebBankingContext())
            {
                User candidate = model.Users.FirstOrDefault(q => q.Username == credentials.Username && q.Password == credentials.Password);

                if (candidate == null) return Ok("Username o password errati");

                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    SigningCredentials = new SigningCredentials(SecurityKeyGenerator.GetSecurityKey(), SecurityAlgorithms.HmacSha256Signature),
                    Expires = DateTime.UtcNow.AddDays(1),
                    Subject = new ClaimsIdentity(
                        new Claim[]
                        {
                            new Claim("Id",candidate.Id.ToString()),
                            new Claim("IsBanker",candidate.IsBanker.ToString())
                        }
                    )
                };

                candidate.LastLogin = DateTime.Now;
                model.SaveChanges();

                SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
                return Ok(tokenHandler.WriteToken(token));

                /*User candidate = model.Users.Where(w => w.Username == credentials.Username && w.Password == credentials.Password).FirstOrDefault();

                if (candidate == null)
                    return Ok("Controlla Username o Password");

                return Ok("LOGIN ESEGUITO");*/



            }
        }

        [Authorize]
        [HttpPost("/logout")]
        public ActionResult Logout()
        {
            var ID_utente = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "Id").Value;
            
            using (WebBankingContext model = new WebBankingContext())
            {
                User candidate = model.Users.FirstOrDefault(q => q.Id.ToString() == ID_utente);
                
                if (candidate == null)
                    return NotFound("USER NON TROVATO, non puoi eseguire il log out");

                candidate.LastLogout = DateTime.Now;
                model.SaveChanges();
                
                return Ok();
            }
        }

    }
}
