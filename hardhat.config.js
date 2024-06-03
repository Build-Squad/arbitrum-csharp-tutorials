require('@nomiclabs/hardhat-ethers');
require('@nomiclabs/hardhat-waffle');
require('dotenv').config();

module.exports = {
  solidity: "0.8.4",
  networks: {
    hardhat: {
      chainId: 1337,
    },
    l1: {
      url: "http://localhost:8545",
      chainId: 1337,
    },
    l2: {
      url: "http://localhost:8547",
      chainId: 412346,
    },
  },
};
