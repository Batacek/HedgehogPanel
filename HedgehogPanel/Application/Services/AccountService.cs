using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HedgehogPanel.Application.Repositories;
using HedgehogPanel.Domain.Entities;

namespace HedgehogPanel.Application.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;

    public AccountService(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<Account?> AuthenticateAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            return null;

        return await _accountRepository.AuthenticateAsync(username, password);
    }

    public async Task<Account?> GetAccountByUsernameAsync(string username)
    {
        return await _accountRepository.GetByUsernameAsync(username);
    }

    public async Task<Account?> GetAccountByIdAsync(Guid userId)
    {
        return await _accountRepository.GetByGuidAsync(userId);
    }

    public async Task<Account> CreateAccountAsync(string username, string email, string password, string? firstName = null, string? middleName = null, string? lastName = null)
    {
        var account = new Account(Guid.NewGuid(), username, email, true, null, firstName, middleName, lastName);
        var success = await _accountRepository.CreateAsync(account, password);
        if (!success) throw new Exception("Failed to create account");
        return account;
    }

    public async Task<bool> UpdateAccountAsync(Account account, string? newPassword = null)
    {
        return await _accountRepository.UpdateAsync(account, newPassword);
    }

    public async Task<bool> DeleteAccountAsync(string username)
    {
        var account = await _accountRepository.GetByUsernameAsync(username);
        if (account == null) return false;
        return await _accountRepository.DeleteAsync(account.Guid);
    }

    public async Task<IReadOnlyList<Account>> ListAccountsAsync(int limit = 100, int offset = 0)
    {
        return await _accountRepository.ListAsync(limit, offset);
    }
}
