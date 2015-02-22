﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BitcoinRpcSharp.Responses;
using BitsharesRpc;
using ApiHost;
using WebDaemonShared;
using WebDaemonSharedTables;
using Monsterer.Request;
using Monsterer.Util;
using Casascius.Bitcoin;
using MySqlDatabase;
using MetaDaemon.Markets;
using MetaData;
using ServiceStack.Text;
using Pathfinder;

namespace MetaDaemon
{
	public interface IDummyDaemon { }

	public partial class MetaDaemonApi : DaemonMySql, IDisposable
	{
		ApiServer<IDummyDaemon> m_server;
		SharedApi<IDummyDaemon> m_api;

		Dictionary<string, MarketBase> m_marketHandlers;
		Dictionary<int, BitsharesAsset> m_allBitsharesAssets;
		List<BitsharesMarket> m_allDexMarkets;

		string m_bitshaaresFeeAccount;
		string m_bitcoinFeeAddress;
		
		public MetaDaemonApi(	RpcConfig bitsharesConfig, RpcConfig bitcoinConfig, 
								string bitsharesAccount,
								string databaseName, string databaseUser, string databasePassword,
								string listenAddress,
								string bitcoinFeeAddress,
								string bitsharesFeeAccount,
								string adminUsernames,
								string masterSiteUrl,
								string masterSiteIp) : 
								base(bitsharesConfig, bitcoinConfig, bitsharesAccount, adminUsernames,
								databaseName, databaseUser, databasePassword)
		{
			m_bitshaaresFeeAccount = bitsharesFeeAccount;
			m_bitcoinFeeAddress = bitcoinFeeAddress;
			m_masterSiteUrl = masterSiteUrl.TrimEnd('/');

			Serialisation.Defaults();

			// don't ban on exception here because we'll only end up banning the webserver!
			m_server = new ApiServer<IDummyDaemon>(new string[] { listenAddress }, null, false, eDdosMaxRequests.Ignore, eDdosInSeconds.One);
			m_server.ExceptionEvent += OnApiException;

			// only allow the main site to post to us
			m_server.SetIpLock(masterSiteIp);

			m_marketHandlers = new Dictionary<string,MarketBase>();

			// get all market pegged assets
			m_allBitsharesAssets = m_bitshares.BlockchainListAssets("", int.MaxValue).Where(a => a.issuer_account_id <= 0).ToDictionary(a => a.id);

			// get all active markets containing those assets
			m_allDexMarkets = m_bitshares.BlockchainListMarkets().Where(m => m.last_error == null &&
																		m_allBitsharesAssets.ContainsKey(m.base_id) &&
																		m_allBitsharesAssets.ContainsKey(m.quote_id)).ToList();

			List<MarketRow> markets = GetAllMarkets();
			foreach (MarketRow r in markets)
			{
				m_marketHandlers[r.symbol_pair] = CreateHandlerForMarket(r);
			}

			m_api = new SharedApi<IDummyDaemon>(m_dataAccess);

			m_server.HandlePostRoute(Routes.kSubmitAddress,				OnSubmitAddress, eDdosMaxRequests.Ignore, eDdosInSeconds.Ignore, false);
			m_server.HandleGetRoute(Routes.kGetAllMarkets,				m_api.OnGetAllMarkets, eDdosMaxRequests.Ignore, eDdosInSeconds.Ignore, false);

			/*m_server.HandlePostRoute(Routes.kGetOrderStatus,			m_api.OnGetOrderStatus, eDdosMaxRequests.Ignore, eDdosInSeconds.Ignore, false);
			m_server.HandlePostRoute(Routes.kGetMarket,					m_api.OnGetMarket, eDdosMaxRequests.Ignore, eDdosInSeconds.Ignore, false);
			m_server.HandlePostRoute(Routes.kGetLastTransactions,		m_api.OnGetLastTransactions, eDdosMaxRequests.Ignore, eDdosInSeconds.Ignore, false);
			m_server.HandlePostRoute(Routes.kGetMyLastTransactions,		m_api.OnGetMyLastTransactions, eDdosMaxRequests.Ignore, eDdosInSeconds.Ignore, false);*/
			

			// internal requests
			m_server.HandleGetRoute(Routes.kPing,						OnPing, eDdosMaxRequests.Ignore, eDdosInSeconds.Ignore, false);
			//m_server.HandlePostRoute(Routes.kGetAllTransactionsSince,	m_api.OnGetAllTransactionsSinceInternal, eDdosMaxRequests.Ignore, eDdosInSeconds.Ignore, false);
		}

		/// <summary>	Starts this object. </summary>
		///
		/// <remarks>	Paul, 25/01/2015. </remarks>
		public override void Start()
		{
			base.Start();

			m_server.Start();
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
		/// resources.
		/// </summary>
		///
		/// <remarks>	Paul, 10/02/2015. </remarks>
		public void Dispose()
		{
			m_server.Dispose();
		}

		/// <summary>	Executes the API exception action. </summary>
		///
		/// <remarks>	Paul, 25/01/2015. </remarks>
		///
		/// <param name="sender">	The sender. </param>
		/// <param name="e">	 	The ExceptionWithCtx to process. </param>
		void OnApiException(object sender, ExceptionWithCtx e)
		{
			if (e.m_e is ApiException)
			{
				ApiException apiE = (ApiException)e.m_e;
				e.m_ctx.Respond<ApiError>(apiE.m_error);
			}
			else if (e.m_ctx != null)
			{
				LogGeneralException(e.m_e.ToString());

				e.m_ctx.Respond<ApiError>(new ApiExceptionGeneral().m_error);
			}
			else
			{
				throw e.m_e;
			}
		}

		/// <summary>	Creates handler for market. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <exception cref="UnexpectedCaseException">	Thrown when an Unexpected Case error condition
		/// 											occurs. </exception>
		///
		/// <param name="market">	The market. </param>
		///
		/// <returns>	The new handler for market. </returns>
		MarketBase CreateHandlerForMarket(MarketRow market)
		{
			CurrencyTypes @base, quote;
			CurrencyHelpers.GetBaseAndQuoteFromSymbolPair(market.symbol_pair, out @base, out quote);

			if (@base == CurrencyTypes.bitBTC && quote == CurrencyTypes.BTC)
			{
				return new InternalMarket(this, market, m_bitshares, m_bitcoin, m_bitsharesAccount, CurrencyTypes.bitBTC);
			}
			else if (@base == CurrencyTypes.BTC && quote == CurrencyTypes.bitUSD)
			{
				return new InternalMarket(this, market, m_bitshares, m_bitcoin, m_bitsharesAccount, CurrencyTypes.bitUSD);
			}
			else
			{
				throw new UnexpectedCaseException();
			}
		}

		/// <summary>	Recompute transaction limits and prices. </summary>
		///
		/// <remarks>	Paul, 30/01/2015. </remarks>
		virtual protected Dictionary<string, MarketRow> RecomputeTransactionLimitsAndPrices()
		{
			// get balances for both wallets
			Dictionary<int, ulong> bitsharesBalances = m_bitshares.WalletAccountBalance(m_bitsharesAccount)[m_bitsharesAccount];
			decimal bitcoinBalance = m_bitcoin.GetBalance();

			// get all markets
			Dictionary<string, MarketRow> allMarkets = GetAllMarkets().ToDictionary(m=>m.symbol_pair);

			// make sure we have handlers for all markets
			foreach (KeyValuePair<string, MarketRow> kvp in allMarkets)
			{
				MarketRow market = kvp.Value;

				if (!m_marketHandlers.ContainsKey(market.symbol_pair))
				{
					m_marketHandlers[market.symbol_pair] = CreateHandlerForMarket(market);
				}
			}

			// update all the limits in our handlers
			foreach (KeyValuePair<string, MarketBase> kvp in m_marketHandlers)
			{
				MarketRow market = allMarkets[kvp.Key];

				// compute new limits and prices for this market
				kvp.Value.ComputeMarketPricesAndLimits(ref market, bitsharesBalances, bitcoinBalance);

				// write them back out
				UpdateMarketInDatabase(market);
			}

			return allMarkets;
		}

		/// <summary>	Handles the price setting. </summary>
		///
		/// <remarks>	Paul, 14/02/2015. </remarks>
		///
		/// <param name="l">	  	The BitsharesLedgerEntry to process. </param>
		/// <param name="handler">	The handler. </param>
		/// <param name="market"> 	The market. </param>
		void HandlePriceSetting(BitsharesLedgerEntry l, MarketBase handler, MarketRow market)
		{
			if (IsPriceSettingTransaction(l))
			{
				try
				{
					// parse
					string[] parts = l.memo.Split(' ');
					if (parts[0] == kSetPricesMemoStart)
					{
						if (parts[1] == market.symbol_pair)
						{
							// setting is for this market!
							decimal basePrice = decimal.Parse(parts[2]);
							decimal quotePrice = decimal.Parse(parts[3]);

							// go do it!
							handler.SetPricesFromSingleUnitQuantities(basePrice, quotePrice);
						}
					}
				}
				catch (Exception e)
				{
					LogGeneralException(e.ToString());
				}
			}
		}

		/// <summary>	Updates this object. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		async public override void Update()
		{
			try
			{
				Dictionary<string, MarketRow> allMarkets = RecomputeTransactionLimitsAndPrices();

				//
				// handle bitshares->bitcoin
				//

				Dictionary<string, BitsharesLedgerEntry> bitsharesDeposits = HandleBitsharesDesposits();

				//
				// handle bitcoin->bitshares
				// 

				List<TransactionSinceBlock> bitcoinDeposits = HandleBitcoinDeposits();

				//
				// process bitshares deposits
				//

				uint siteLastTid = m_dataAccess.GetSiteLastTransactionUid();
				
				foreach (KeyValuePair<string, BitsharesLedgerEntry> kvpDeposit in bitsharesDeposits)
				{
					// figure out which market each deposit belongs to
					foreach (KeyValuePair<string, MarketBase> kvpHandler in m_marketHandlers)
					{
						BitsharesLedgerEntry l = kvpDeposit.Value;
						MarketRow m = allMarkets[kvpHandler.Key];
						BitsharesAsset depositAsset = m_allBitsharesAssets[l.amount.asset_id];

						if (IsDepositForMarket(l.memo, m.symbol_pair))
						{
							// make sure the deposit is for this market!
							if (kvpHandler.Value.CanDepositAsset( CurrencyHelpers.FromBitsharesSymbol(depositAsset.symbol) ))
							{
								kvpHandler.Value.HandleBitsharesDeposit(kvpDeposit);
							}
						}
						else 
						{
							HandlePriceSetting(l, kvpHandler.Value, allMarkets[kvpHandler.Key]);
						}
					}
				}

				//
				// process bitcoin deposits
				// 

				foreach (TransactionSinceBlock deposit in bitcoinDeposits)
				{
					// figure out which market each deposit belongs to
					foreach (KeyValuePair<string, MarketBase> kvpHandler in m_marketHandlers)
					{
						if (IsDepositForMarket(deposit.Address, allMarkets[kvpHandler.Key].symbol_pair))
						{
							kvpHandler.Value.HandleBitcoinDeposit(deposit);
						}
					}
				}

				if (m_bitcoinFeeAddress != null && m_bitshaaresFeeAccount != null)
				{
					// collect our fees
					foreach (KeyValuePair<string, MarketBase> kvpHandler in m_marketHandlers)
					{
						kvpHandler.Value.CollectFees(m_bitcoinFeeAddress, m_bitshaaresFeeAccount);
					}
				}

				//
				// push any new transactions, make sure site acknowledges receipt
				//

				uint latestTid = m_dataAccess.GetLastTransactionUid();
				if (latestTid > siteLastTid)
				{
					List<TransactionsRow> newTrans = m_dataAccess.GetAllTransactionsSince(siteLastTid);
					string result = await ApiPush<List<TransactionsRow>>(Routes.kPushTransactions, newTrans);
					if (bool.Parse(result))
					{
						m_dataAccess.UpdateSiteLastTransactionUid(latestTid);
					}
				}

				//
				// push market updates
				//

				foreach (KeyValuePair<string, MarketBase> kvpHandler in m_marketHandlers)
				{
					if (kvpHandler.Value.m_IsDirty)
					{
						ApiPush<MarketRow>(Routes.kPushMarket, kvpHandler.Value.m_Market);
					}
				}
			}
			catch (Exception e)
			{
				LogGeneralException(e.ToString());
			}
		}

		/// <summary>	Gets the API server. </summary>
		///
		/// <value>	The m API server. </value>
		public ApiServer<IDummyDaemon> m_ApiServer
		{
			get { return m_server; }
		}

		/// <summary>	Gets all dex markets. </summary>
		///
		/// <value>	The m all dex markets. </value>
		public List<BitsharesMarket> m_AllDexMarkets
		{
			get { return m_allDexMarkets; }
		}

		/// <summary>	Gets all dex assets. </summary>
		///
		/// <value>	The m all dex assets. </value>
		public Dictionary<int, BitsharesAsset> m_AllBitsharesAssets
		{
			get { return m_allBitsharesAssets; }
		}
	}
}
