using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace MessengerAPI.Services
{
    public static class WebDriverExtensions
    {
        public static IWebElement FindElement(this IWebDriver driver, By by, int timeoutInSeconds)
        {
            if (timeoutInSeconds > 0)
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutInSeconds));
                return wait.Until(drv => drv.FindElement(by));
            }
            return driver.FindElement(by);
        }
    }

    public interface ISingleMessengerService : IDisposable
    {
        Guid id { get; }
        DateTime aliveUntil { get; set; }
        TaskStatus TaskStatus { get; set; }

        void KeepAlive();
        void LoginWithCredentials(string username, string password);
        void CompleteLogin(string code);
        void SendMessage(string to, string message);

    }

    public class SingleMessengerService : ISingleMessengerService
    {
        public SingleMessengerService(Guid id)
        {
            this.id = id;
            KeepAlive();
        }
        object statusLock = new object();
        private TaskStatus _taskStatus = TaskStatus.Completed;
        public TaskStatus TaskStatus 
        { 
            get
            {
                return _taskStatus;                    
            } 
            set
            {
                _taskStatus = value;
            } 
        }
        public Guid id { get; }
        public DateTime aliveUntil { get; set; }
        private IWebDriver _driver;

        public void KeepAlive()
        {
            aliveUntil = DateTime.Now.AddMinutes(5);
        }

        private void CreateDriverIfNeeded()
        {
            if(null != _driver)
                return;
            var options = new OpenQA.Selenium.Chrome.ChromeOptions();
            options.AddArguments(new List<string>() {"headless", "no-sandbox", "disable-dev-shm-usage"});
            var workingDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            ChromeDriverService service= ChromeDriverService.CreateDefaultService(workingDir);
            service.Port = 40785;
            _driver = new OpenQA.Selenium.Chrome.ChromeDriver(service, options);
        }

        protected void Dispose(bool disposing)
        {
            if(disposing){
                _driver?.Dispose();
                _driver = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async void LoginWithCredentials(string username, string password)
        {
            lock(statusLock)
            {
                if(TaskStatus.InProgress == TaskStatus)
                    return;

                TaskStatus = TaskStatus.InProgress;
            }

            CreateDriverIfNeeded();

            await Task.Run(() => 
            {
                _driver.Navigate().GoToUrl("https://messenger.com/login");
                if(_driver.Url.Contains("/login")){
                    IWebElement emailInput = _driver.FindElement(By.Id("email"));
                    IWebElement passwordInput = _driver.FindElement(By.Id("pass"));
                    emailInput.SendKeys(username);
                    passwordInput.SendKeys(password);
                    passwordInput.SendKeys(Keys.Enter);
                }
                if(_driver.Url.Contains("/password")){

                    //Login failed
                    Console.WriteLine("Okay login failed, no idea what to do now.");
                    lock(statusLock)
                    {
                        TaskStatus = TaskStatus.Failed;
                    }
                    return;
                }
                if(_driver.Url.Contains("/login")){
                    // Confirmation needed.
                    var aElements = _driver.FindElements(By.TagName("a"));
                    var continueBtn = aElements.FirstOrDefault((IWebElement element) => {
                        if(element.Text.IndexOf("continue",StringComparison.OrdinalIgnoreCase) >= 0){
                            return true;
                        }
                        return false;
                    });
                    if(null != continueBtn)
                        continueBtn.Click();

                    lock(statusLock)
                    {
                        TaskStatus = TaskStatus.ConfirmationRequired;
                    }
                    return;
                }

                lock(statusLock)
                {
                    TaskStatus = TaskStatus.Completed;
                }
            }).ConfigureAwait(false);
        }

        public static IWebElement FindElementIfExists(IWebDriver driver, By by)
        {
            var elements = driver.FindElements(by);
            return (elements.Count >=1) ? elements.First() : null;
        }

        public async void CompleteLogin(string code)
        {
            lock(statusLock)
            {
                if(TaskStatus.InProgress == TaskStatus)
                    return;

                TaskStatus = TaskStatus.InProgress;
            }

            await Task.Run(() => 
            {
                if(_driver.Url.Contains("facebook.com/"))
                {
                    var loginCodeField = _driver.FindElement(By.Id("approvals_code"));
                    loginCodeField.SendKeys(code);
                    loginCodeField.SendKeys(Keys.Enter);

                    var submitButton = FindElementIfExists(_driver, By.Id("checkpointSubmitButton"));
                    while(null != submitButton)
                    {
                        if(_driver.FindElements(By.Id("approvals_code")).Count > 0)
                        {
                            lock(statusLock)
                            {
                                TaskStatus = TaskStatus.ConfirmationRequired;
                            }
                            return;
                        }
                        // Save browser?
                        _driver.FindElements(By.TagName("input")).LastOrDefault((IWebElement el) => {
                            return el.GetAttribute("type") == "radio";
                        })?.Click();

                        submitButton.SendKeys(Keys.Enter);
                        submitButton = FindElementIfExists(_driver, By.Id("checkpointSubmitButton"));
                        if(null == submitButton)
                        {
                            var aElements = _driver.FindElements(By.TagName("a"));
                            var continueBtn = aElements.FirstOrDefault((IWebElement element) => {
                                if(element.Text.IndexOf("continue",StringComparison.OrdinalIgnoreCase) >= 0){
                                    return true;
                                }
                                return false;
                            });
                            if(null != continueBtn)
                            {
                                submitButton = continueBtn;
                            }
                        }
                    }
                    if(!_driver.Url.Contains("messenger.com/"))
                    {
                        lock(statusLock)
                        {
                            TaskStatus = TaskStatus.Failed;
                        }
                    }
                }
                else
                {
                    lock(statusLock)
                    {
                        TaskStatus = TaskStatus.Failed;
                    }
                }

                lock(statusLock)
                {
                    TaskStatus = TaskStatus.Completed;
                }
            }).ConfigureAwait(false);
        }

        public async void SendMessage(string to, string message)
        {
            lock(statusLock)
            {
                if(TaskStatus.InProgress == TaskStatus)
                    return;

                TaskStatus = TaskStatus.InProgress;
            }

            await Task.Run(() => 
            {
                _driver.Navigate().GoToUrl("https://messenger.com");
                var searchBox = _driver.FindElements(By.TagName("input")).FirstOrDefault((IWebElement el) => 
                {
                    return el.GetAttribute("placeholder") == "Search Messenger";
                });
                if(null == searchBox)
                {
                    lock(statusLock)
                    {
                        TaskStatus = TaskStatus.Failed;
                    }
                    return;
                }
                searchBox.SendKeys(to);
                Thread.Sleep(TimeSpan.FromSeconds(2));
                var targetPerson = _driver.FindElement(By.XPath("/descendant::a/descendant::*[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), '" + to.ToLower() + "') and not(contains(text(), '\"'))]"), 10);
                if(null == targetPerson)
                {
                    lock(statusLock)
                    {
                        TaskStatus = TaskStatus.Failed;
                    }
                    return;
                }

                targetPerson.Click();
                Thread.Sleep(TimeSpan.FromSeconds(2));
                var messageBox = _driver.FindElement(By.XPath("/descendant::*[@aria-label='Type a message...']"), 10);
                if(null == messageBox)
                {
                    lock(statusLock)
                    {
                        TaskStatus = TaskStatus.Failed;
                    }
                    return;
                }
                messageBox.Click();
                messageBox.SendKeys(message);
                messageBox.SendKeys(Keys.Enter);

                lock(statusLock)
                {
                    TaskStatus = TaskStatus.Completed;
                }
            }).ConfigureAwait(false);
        }
    }
}