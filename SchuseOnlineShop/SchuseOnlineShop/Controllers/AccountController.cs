﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SchuseOnlineShop.Models;
using SchuseOnlineShop.Services.Interfaces;
using SchuseOnlineShop.ViewModels.Account;
using SchuseOnlineShop.ViewModels.Cart;

namespace SchuseOnlineShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;
        private readonly ICartService _cartService;
        private readonly ICrudService<Cart> _crudService;

        public AccountController(UserManager<AppUser> userManager, 
                                SignInManager<AppUser> signInManager,
                                RoleManager<IdentityRole> roleManager, 
                                IEmailService emailService, 
                                ICartService cartService, 
                                ICrudService<Cart> crudService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _cartService = cartService;
            _crudService = crudService;
        }


        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model)
        {
            try
            {
                if (!ModelState.IsValid) return RedirectToAction("Login", model);

                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Email or password is wrong");
                    RedirectToAction("Index", model);
                }

                var res = await _signInManager.PasswordSignInAsync(user, model.Password, model.IsRemember, false);

                if (!res.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Email or password is wrong");
                    RedirectToAction("Index", model);
                }

                List<CartVM> cartVMs = new();

                Cart dbCart = await _cartService.GetByUserIdAsync(user.Id);

                if (dbCart is not null)
                {
                    List<CartProduct> cartProducts = await _cartService.GetAllByCartIdAsync(dbCart.Id);
                    foreach (var cartProduct in cartProducts)
                    {
                        cartVMs.Add(new CartVM
                        {
                            ProductId = cartProduct.ProductId,
                            Count = cartProduct.Count
                        });
                    }

                    Response.Cookies.Append("basket", JsonConvert.SerializeObject(cartVMs));
                }


                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
                return View();
            }
        }




        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId is null || token is null) return BadRequest();
            AppUser user = await _userManager.FindByIdAsync(userId);
            if (user is null) return NotFound();

            await _userManager.ConfirmEmailAsync(user, token);

            await _signInManager.SignInAsync(user, user.IsRemember);

            List<CartVM> cartVMs = new();
            Cart dbCart = await _cartService.GetByUserIdAsync(userId);

            if (dbCart is not null)
            {
                List<CartProduct> cartProducts = await _cartService.GetAllByCartIdAsync(dbCart.Id);
                foreach (var cartProduct in cartProducts)
                {
                    cartVMs.Add(new CartVM
                    {
                        ProductId = cartProduct.ProductId,
                        Count = cartProduct.Count
                    });
                }

                Response.Cookies.Append("basket", JsonConvert.SerializeObject(cartVMs));
            }



            Response.Cookies.Append("basket", JsonConvert.SerializeObject(cartVMs));

            return RedirectToAction("Index", "Home");
        }

        public IActionResult VerifyEmail()
        {
            return View();
        }


        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return RedirectToAction(nameof(Index));
                }

                Random random = new();

                AppUser newUser = new()
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    UserName = model.FirstName
                };

                IdentityResult result = await _userManager.CreateAsync(newUser, model.Password);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    //TempData["errors"] = model.ErrorMessages;
                    return RedirectToAction("Index", model);
                }

                string token = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);
                string link = Url.Action(nameof(ConfirmEmail), "Account", new { userId = newUser.Id, token },
                                            Request.Scheme, Request.Host.ToString());


                string subject = "Register Confirmation";
                string html = string.Empty;

                using (StreamReader reader = new("wwwroot/templates/verify.html"))
                {
                    html = reader.ReadToEnd();
                }

                html = html.Replace("{{link}}", link);

                _emailService.Send(newUser.Email, subject, html);

                await _userManager.AddToRoleAsync(newUser, "Member");

                return RedirectToAction(nameof(VerifyEmail));

            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
                return RedirectToAction("Index", model);
            }

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(string userId)
        {
            await _signInManager.SignOutAsync();

            List<CartVM> carts = _cartService.GetDatasFromCookie();

            if (carts.Count != 0)
            {
                Cart dbCart = await _cartService.GetByUserIdAsync(userId);
                if (dbCart == null)
                {
                    dbCart = new()
                    {
                        AppUserId = userId,
                        CartProducts = new List<CartProduct>()
                    };
                    foreach (var cart in carts)
                    {
                        dbCart.CartProducts.Add(new CartProduct()
                        {
                            ProductId = cart.ProductId,
                            CartId = dbCart.Id,
                            Count = cart.Count
                        });
                    }
                    await _crudService.CreateAsync(dbCart);

                }
                else
                {
                    List<CartProduct> cartProducts = new List<CartProduct>();
                    foreach (var cart in carts)
                    {
                        cartProducts.Add(new CartProduct()
                        {
                            ProductId = cart.ProductId,
                            CartId = dbCart.Id,
                            Count = cart.Count
                        });
                    }
                    dbCart.CartProducts = cartProducts;
                    await _crudService.SaveAsync();

                }
                Response.Cookies.Delete("basket");

            }

            return RedirectToAction("Index", "Home");
        }


        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordVM forgotPassword)
        {
            if (!ModelState.IsValid) return View();

            AppUser existUser = await _userManager.FindByEmailAsync(forgotPassword.Email);

            if (existUser is null)
            {
                ModelState.AddModelError("Email", "User not found");
                return View();
            }

            string token = await _userManager.GeneratePasswordResetTokenAsync(existUser);

            string link = Url.Action(nameof(ConfirmEmail), "Account", new { userId = existUser.Id, token }, Request.Scheme, Request.Host.ToString());


            string subject = "Verify password reset email";

            string html = string.Empty;

            using (StreamReader reader = new StreamReader("wwwroot/templates/verify.html"))
            {
                html = reader.ReadToEnd();
            }

            html = html.Replace("{{link}}", link);
            html = html.Replace("{{headerText}}", "Hello P135");

            _emailService.Send(existUser.Email, subject, html);
            return RedirectToAction(nameof(VerifyEmail));
        }
    }
}